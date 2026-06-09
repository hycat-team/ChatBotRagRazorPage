using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize(Roles = "HeadOfDepartment")]
    public class HodController : Controller
    {
        private readonly IAppUserRepository _userRepository;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public HodController(IAppUserRepository userRepository, ApplicationDbContext context, IAuditLogService auditLogService)
        {
            _userRepository = userRepository;
            _context = context;
            _auditLogService = auditLogService;
        }

        private async Task<AppUser?> GetCurrentUser()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                return await _userRepository.GetByIdAsync(userId);
            }
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            // Lấy danh sách môn học thuộc bộ môn của HOD
            var subjects = await _context.Subjects
                .Where(s => s.DepartmentId == user.DepartmentId)
                .ToListAsync();

            return View(subjects);
        }
    }
}
