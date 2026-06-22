using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Document
{
    [Authorize(Roles = "Student")]
    public class BrowseModel : PageModel
    {
        private readonly RagChatbot.Business.Interfaces.IDocumentService _documentService;
        private readonly RagChatbot.Business.Interfaces.ISubjectService _subjectService;
        private readonly RagChatbot.Business.Interfaces.IAppUserService _userService;

        public BrowseModel(
            RagChatbot.Business.Interfaces.IDocumentService documentService,
            RagChatbot.Business.Interfaces.ISubjectService subjectService,
            RagChatbot.Business.Interfaces.IAppUserService userService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _userService = userService;
        }

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.DocumentDto> Documents { get; set; } = new List<RagChatbot.Business.DTOs.DocumentDto>();

        public async Task OnGetAsync(int? subjectId, string searchString)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isPremium = false;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _userService.GetByIdAsync(userId);
                if (user != null)
                {
                    isPremium = user.Subscription == "Premium";
                }
            }
            ViewData["IsPremium"] = isPremium;

            var allSubjects = await _subjectService.GetAllAsync();
            var subjects = allSubjects.OrderBy(s => s.Name).ToList();

            var allDocs = await _documentService.GetAllAsync();
            var query = allDocs.Where(d => d.Status == "Indexed" && d.IsActive);

            if (subjectId.HasValue && subjectId.Value > 0)
            {
                query = query.Where(d => d.SubjectId == subjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                query = query.Where(d => d.FileName.ToLower().Contains(keyword) ||
                                     (d.DisplayName != null && d.DisplayName.ToLower().Contains(keyword)));
            }

            Documents = query.OrderByDescending(d => d.UploadedAt).ToList();

            ViewData["Subjects"] = subjects;
            ViewData["SelectedSubjectId"] = subjectId;
            ViewData["SearchString"] = searchString;
        }
    }
}
