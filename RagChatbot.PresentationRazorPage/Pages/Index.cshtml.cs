using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RagChatbot.PresentationRazorPage.ViewModels;

namespace RagChatbot.PresentationRazorPage.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IGoogleDriveService _driveService;

        public IndexModel(
            ISubjectService subjectService,
            IDocumentService documentService,
            IGoogleDriveService driveService)
        {
            _subjectService = subjectService;
            _documentService = documentService;
            _driveService = driveService;
        }

        public HomeIndexViewModel ViewModel { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (User.IsInRole("HeadOfDepartment"))
            {
                return RedirectToPage("/Document/Index");
            }

            // Student and Admin sees all subjects
            var subjects = await _subjectService.GetAllAsync();

            ViewModel = new HomeIndexViewModel
            {
                Subjects = subjects
            };
            return Page();
        }

        public async Task<IActionResult> OnGetTestDbAsync()
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
            return new JsonResult(results);
        }
    }
}
