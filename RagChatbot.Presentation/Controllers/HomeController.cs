using Microsoft.AspNetCore.Mvc;
using RagChatbot.Business.Services;
using RagChatbot.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IGoogleDriveService _driveService;

        public HomeController(
            ISubjectService subjectService,
            IDocumentService documentService,
            IGoogleDriveService driveService)
        {
            _subjectService = subjectService;
            _documentService = documentService;
            _driveService = driveService;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (User.IsInRole("HeadOfDepartment"))
            {
                return RedirectToAction("Index", "Document");
            }

            // Student and Admin sees all subjects
            var subjects = await _subjectService.GetAllAsync();

            var viewModel = new RagChatbot.Presentation.ViewModels.HomeIndexViewModel
            {
                Subjects = subjects
            };
            return View(viewModel);
        }

        public async Task<IActionResult> TestDb()
        {
            var docs = await _documentService.GetAllAsync();
            var results = new List<object>();
            foreach (var d in docs)
            {
                var chunkCount = await _documentService.GetChunksByDocumentIdAsync(d.Id);
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
