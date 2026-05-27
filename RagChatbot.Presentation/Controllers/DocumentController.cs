using RagChatbot.DataAccess.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Repositories;
using RagChatbot.Business.Services;
using RagChatbot.Business.Interfaces;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentRepository _docRepo;
        private readonly ISubjectRepository _subjectRepo;
        private readonly IDocumentChunkRepository _chunkRepo;
        private readonly IChatSessionRepository _sessionRepo;
        private readonly IChatMessageRepository _messageRepo;
        private readonly IWebHostEnvironment _env;

        public DocumentController(
            IDocumentRepository docRepo,
            ISubjectRepository subjectRepo,
            IDocumentChunkRepository chunkRepo,
            IChatSessionRepository sessionRepo,
            IChatMessageRepository messageRepo,
            IWebHostEnvironment env)
        {
            _docRepo = docRepo;
            _subjectRepo = subjectRepo;
            _chunkRepo = chunkRepo;
            _sessionRepo = sessionRepo;
            _messageRepo = messageRepo;
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
            var subjects = await _subjectRepo.Query().Where(s => s.UserId == userId).Include(s => s.Documents).ToListAsync();
            var subjectIds = subjects.Select(s => s.Id).ToList();
            var documents = await _docRepo.Query().Where(d => subjectIds.Contains(d.SubjectId)).Include(d => d.Subject).Include(d => d.DocumentChunks).ToListAsync();
            
            ViewBag.Subjects = subjects;
            return View(documents);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(
            int subjectId,
            IFormFile file,
            [FromServices] IGoogleDriveService driveService,
            [FromServices] ILocalStorageService localStorage)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectRepo.GetByIdAsync(subjectId);
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

            var document = new Document
            {
                SubjectId = subjectId,
                FileName = file.FileName,
                FilePath = filePath,
                Status = "Pending",
                UploadedAt = DateTime.UtcNow
            };

            await _docRepo.AddAsync(document);
            await _docRepo.SaveChangesAsync();

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
            await _subjectRepo.AddAsync(new Subject { Code = code, Name = name, UserId = userId });
            await _subjectRepo.SaveChangesAsync();

            TempData["Success"] = "Subject created.";
            return RedirectToAction(nameof(Index));
        }

        // ─── Quản lý Subject ─────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> RenameSubject(int id, string name)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectRepo.GetByIdAsync(id);
            if (subject == null || subject.UserId != userId) return Json(new { success = false, message = "Subject not found or unauthorized." });

            subject.Name = name.Trim();
            await _subjectRepo.SaveChangesAsync();
            return Json(new { success = true, name = subject.Name });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectRepo.Query()
                .Include(s => s.Documents)
                    .ThenInclude(d => d.DocumentChunks)
                .Include(s => s.ChatSessions)
                    .ThenInclude(cs => cs.Messages)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null || subject.UserId != userId) return Json(new { success = false, message = "Subject not found or unauthorized." });

            // Cascade: xóa chunks → documents → chat messages → sessions → subject
            foreach (var doc in subject.Documents)
                _chunkRepo.RemoveRange(doc.DocumentChunks);

            _docRepo.RemoveRange(subject.Documents);

            foreach (var session in subject.ChatSessions)
                _messageRepo.RemoveRange(session.Messages);

            _sessionRepo.RemoveRange(subject.ChatSessions);
            _subjectRepo.Remove(subject);

            await _subjectRepo.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ─── Quản lý Document ────────────────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _docRepo.Query()
                .Include(d => d.Subject)
                .Include(d => d.DocumentChunks)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null || document.Subject?.UserId != userId) return Json(new { success = false, message = "Document not found or unauthorized." });

            _chunkRepo.RemoveRange(document.DocumentChunks);
            _docRepo.Remove(document);
            await _docRepo.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ─── API cho Chat Page ────────────────────────────────────────────────

        /// <summary>Trả về danh sách tài liệu đã Indexed của 1 subject (dùng cho Document Filter panel)</summary>
        [HttpGet]
        public async Task<IActionResult> GetSubjectDocuments(int subjectId)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectRepo.GetByIdAsync(subjectId);
            if (subject == null || subject.UserId != userId) return Json(new List<object>());

            var docs = await _docRepo.Query()
                .Where(d => d.SubjectId == subjectId && d.Status == "Indexed")
                .Select(d => new { d.Id, d.FileName, d.UploadedAt })
                .OrderBy(d => d.FileName)
                .ToListAsync();

            return Json(docs);
        }
    }
}
