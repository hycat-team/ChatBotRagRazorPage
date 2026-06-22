using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.Pages.Hod
{
    [Authorize(Roles = "HeadOfDepartment")]
    public class IndexModel : PageModel
    {
        private readonly IAppUserService _userService;
        private readonly ISubjectService _subjectService;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(IAppUserService userService, ISubjectService subjectService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _subjectService = subjectService;
            _auditLogService = auditLogService;
        }

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.SubjectDto> Subjects { get; set; } = default!;

        private async Task<RagChatbot.Business.DTOs.AppUserDto> GetCurrentUser()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                return await _userService.GetByIdAsync(userId);
            }
            return null!;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            Subjects = await _subjectService.GetByDepartmentIdAsync(user.DepartmentId ?? 0);

            return Page();
        }

        public async Task<IActionResult> OnGetSubjectsDataAsync()
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            var subjects = await _subjectService.GetByDepartmentIdAsync(user.DepartmentId ?? 0);
            var subjectData = subjects.Select(s => new
            {
                s.Code,
                s.Name,
                s.IsActive
            });

            return new JsonResult(subjectData);
        }
    }
}
