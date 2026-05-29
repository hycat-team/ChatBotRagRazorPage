using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.Business.Services;
using RagChatbot.Business.Interfaces;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChatService _chatService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public DocumentController(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChatService chatService,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chatService = chatService;
            _env = env;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int userId) ? userId : 0;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var subjects = await _subjectService.GetAllByUserIdAsync(userId);
            var subjectIds = subjects.Select(s => s.Id).ToList();
            
            var documents = new List<RagChatbot.Business.DTOs.DocumentDto>();
            foreach (var subjectId in subjectIds)
            {
                var docs = await _documentService.GetBySubjectIdAsync(subjectId);
                documents.AddRange(docs);
            }
            
            var viewModel = new RagChatbot.Presentation.ViewModels.DocumentIndexViewModel
            {
                Documents = documents,
                Subjects = subjects
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(
            int subjectId,
            IFormFile file,
            [FromServices] IGoogleDriveService driveService,
            [FromServices] ILocalStorageService localStorage)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectService.GetByIdAsync(subjectId);
            if (subject == null || subject.UserId != userId)
            {
                TempData["Error"] = "Invalid subject.";
                return RedirectToAction(nameof(Index));
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid file.";
                return RedirectToAction(nameof(Index));
            }

            string filePath;
            string storageInfo;

            try
            {
                using var stream = file.OpenReadStream();
                filePath = await driveService.UploadFileAsync(stream, file.FileName, file.ContentType);
                storageInfo = "Google Drive";
            }
            catch (Exception driveEx)
            {
                try
                {
                    using var stream = file.OpenReadStream();
                    filePath = await localStorage.SaveFileAsync(stream, file.FileName);
                    storageInfo = "local server (Google Drive unavailable)";
                }
                catch (Exception localEx)
                {
                    TempData["Error"] = $"Upload failed. Google Drive: {driveEx.Message} | Local: {localEx.Message}";
                    return RedirectToAction(nameof(Index));
                }
            }

            var document = new RagChatbot.Business.DTOs.CreateDocumentDto
            {
                SubjectId = subjectId,
                FileName = file.FileName,
                FilePath = filePath,
                Status = "Pending",
                UploadedAt = DateTime.UtcNow
            };

            await _documentService.AddAsync(document);

            TempData["Success"] = $"Document uploaded successfully to {storageInfo} and is pending processing.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Code and Name are required.";
                return RedirectToAction(nameof(Index));
            }

            var userId = GetCurrentUserId();
            await _subjectService.AddAsync(new RagChatbot.Business.DTOs.CreateSubjectDto { Code = code, Name = name, UserId = userId });

            TempData["Success"] = "Subject created.";
            return RedirectToAction(nameof(Index));
        }

        // ─── Quản lý Subject ─────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> RenameSubject(int id, string name)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectService.GetByIdAsync(id);
            if (subject == null || subject.UserId != userId) return Json(new { success = false, message = "Subject not found or unauthorized." });

            subject.Name = name.Trim();
            await _subjectService.UpdateAsync(subject);
            return Json(new { success = true, name = subject.Name });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectService.GetByIdAsync(id);

            if (subject == null || subject.UserId != userId) return Json(new { success = false, message = "Subject not found or unauthorized." });

            // Note: EF Core cascade delete will handle related documents, chunks, sessions, and messages if configured properly.
            // If manual cascade is required, it should be implemented in the SubjectService.
            await _subjectService.DeleteAsync(id);
            return Json(new { success = true });
        }

        // ─── Quản lý Document ────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);

            if (document == null) return Json(new { success = false, message = "Document not found." });
            
            var subject = await _subjectService.GetByIdAsync(document.SubjectId);
            if (subject == null || subject.UserId != userId) return Json(new { success = false, message = "Document unauthorized." });

            await _documentService.DeleteAsync(id);
            return Json(new { success = true });
        }

        // ─── API cho Chat Page ────────────────────────────────────────────────

        /// <summary>Trả về danh sách tài liệu đã Indexed của 1 subject (dùng cho Document Filter panel)</summary>
        [HttpGet]
        public async Task<IActionResult> GetSubjectDocuments(int subjectId)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectService.GetByIdAsync(subjectId);
            if (subject == null || subject.UserId != userId) return Json(new List<object>());

            var docs = await _documentService.GetBySubjectIdAsync(subjectId);
            var indexedDocs = docs
                .Where(d => d.Status == "Indexed")
                .Select(d => new { d.Id, d.FileName, d.UploadedAt })
                .OrderBy(d => d.FileName)
                .ToList();

            return Json(indexedDocs);
        }
    }
}
