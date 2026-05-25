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
        public async Task<IActionResult> Upload(int subjectId, IFormFile file, [FromServices] IGoogleDriveService driveService)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid file.";
                return RedirectToAction(nameof(Index));
            }

            try 
            {
                // Upload to Google Drive
                using var stream = file.OpenReadStream();
                var fileId = await driveService.UploadFileAsync(stream, file.FileName, file.ContentType);

                // Save to database
                var document = new Document
                {
                    SubjectId = subjectId,
                    FileName = file.FileName,
                    FilePath = fileId, // Store Google Drive File ID here
                    Status = "Pending",
                    UploadedAt = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Document uploaded successfully to Google Drive and is pending processing.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error uploading to Google Drive: " + ex.Message;
            }

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
