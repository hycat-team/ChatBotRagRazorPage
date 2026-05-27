using RagChatbot.DataAccess.Interfaces;
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
    public class HomeController : Controller
    {
        private readonly ISubjectRepository _subjectRepo;
        private readonly IDocumentRepository _docRepo;
        private readonly IDocumentChunkRepository _chunkRepo;
        private readonly IGoogleDriveService _driveService;

        public HomeController(
            ISubjectRepository subjectRepo,
            IDocumentRepository docRepo,
            IDocumentChunkRepository chunkRepo,
            IGoogleDriveService driveService)
        {
            _subjectRepo = subjectRepo;
            _docRepo = docRepo;
            _chunkRepo = chunkRepo;
            _driveService = driveService;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var subjects = await _subjectRepo.Query().Where(s => s.UserId == userId).ToListAsync();
            return View(subjects);
        }

        public async Task<IActionResult> TestDb()
        {
            var docs = await _docRepo.GetAllAsync();
            var results = new List<object>();
            foreach (var d in docs)
            {
                var chunkCount = await _chunkRepo.Query().CountAsync(c => c.DocumentId == d.Id);
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
