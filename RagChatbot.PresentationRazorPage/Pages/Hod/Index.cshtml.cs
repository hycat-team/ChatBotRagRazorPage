using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Hod
{
    [Authorize(Roles = "HeadOfDepartment")]
    public class IndexModel : PageModel
    {
        private readonly IAppUserRepository _userRepository;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(IAppUserRepository userRepository, ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _userRepository = userRepository;
            _context = context;
            _auditLogService = auditLogService;
        }

        public System.Collections.Generic.List<Subject> Subjects { get; set; }

        private async Task<AppUser> GetCurrentUser()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                return await _userRepository.GetByIdAsync(userId);
            }
            return null;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            Subjects = await _context.Subjects
                .Where(s => s.DepartmentId == user.DepartmentId && s.IsActive)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetSubjectsDataAsync()
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            var subjects = await _context.Subjects
                .Where(s => s.DepartmentId == user.DepartmentId && s.IsActive)
                .Select(s => new {
                    s.Code,
                    s.Name,
                    s.IsActive
                })
                .ToListAsync();

            return new JsonResult(subjects);
        }
    }
}
