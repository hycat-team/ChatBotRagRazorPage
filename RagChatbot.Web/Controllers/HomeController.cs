using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Web.Data;
using RagChatbot.Web.Services;

namespace RagChatbot.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGoogleDriveService _driveService;

        public HomeController(ApplicationDbContext context, IGoogleDriveService driveService)
        {
            _context = context;
            _driveService = driveService;
        }

        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return View(subjects);
        }

        public async Task<IActionResult> TestDb()
        {
            var docs = await _context.Documents.ToListAsync();
            var results = new List<object>();
            foreach (var d in docs)
            {
                var chunkCount = await _context.DocumentChunks.CountAsync(c => c.DocumentId == d.Id);
                results.Add(new
                {
                    d.Id,
                    d.FileName,
                    d.FilePath,
                    d.Status,
                    ChunkCount = chunkCount
                });
            }
            return Json(results);
        }
    }
}
