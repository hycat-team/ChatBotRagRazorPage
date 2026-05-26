using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Web.Data;
using RagChatbot.Web.Models.Entities;
using RagChatbot.Web.Services;

namespace RagChatbot.Web.Controllers
{
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var documents = await _context.Documents.Include(d => d.Subject).Include(d => d.DocumentChunks).ToListAsync();
            var subjects = await _context.Subjects.ToListAsync();
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
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid file.";
                return RedirectToAction(nameof(Index));
            }

            string filePath;
            string storageInfo;

            try
            {
                // Attempt 1: Upload to Google Drive
                using var stream = file.OpenReadStream();
                filePath = await driveService.UploadFileAsync(stream, file.FileName, file.ContentType);
                storageInfo = "Google Drive";
            }
            catch (Exception driveEx)
            {
                // Attempt 2: Fallback — save to local server storage
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

            // Save to database regardless of storage backend
            var document = new Document
            {
                SubjectId = subjectId,
                FileName = file.FileName,
                FilePath = filePath,
                Status = "Pending",
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

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

            _context.Subjects.Add(new Subject { Code = code, Name = name });
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Subject created.";
            return RedirectToAction(nameof(Index));
        }
    }
}
