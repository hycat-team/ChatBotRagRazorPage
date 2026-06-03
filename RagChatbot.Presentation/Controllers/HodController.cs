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
            if (int.TryParse(User.Identity!.Name, out int userId))
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
                .Include(s => s.Assignments)
                .ThenInclude(a => a.Lecturer)
                .Where(s => s.DepartmentId == user.DepartmentId)
                .ToListAsync();

            ViewBag.Lecturers = await _context.AppUsers
                .Where(u => u.Role == "Lecturer") // Có thể giới hạn Lecturer thuộc bộ môn nếu muốn
                .ToListAsync();

            return View(subjects);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject(string code, string name)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
            {
                var subject = new Subject
                {
                    Code = code,
                    Name = name,
                    UserId = user.Id,
                    DepartmentId = user.DepartmentId
                };
                
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                
                await _auditLogService.LogAsync(user.Id, "Create Subject", subject.Id.ToString(), $"Code: {code}, Name: {name}");
                TempData["Success"] = "Thêm môn học thành công.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AssignLecturer(int subjectId, int lecturerId)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject != null && subject.DepartmentId == user.DepartmentId)
            {
                var existingAssign = await _context.SubjectAssignments
                    .FirstOrDefaultAsync(sa => sa.SubjectId == subjectId && sa.LecturerId == lecturerId);

                if (existingAssign == null)
                {
                    _context.SubjectAssignments.Add(new SubjectAssignment { SubjectId = subjectId, LecturerId = lecturerId });
                    await _context.SaveChangesAsync();
                    await _auditLogService.LogAsync(user.Id, "Assign Lecturer", subjectId.ToString(), $"LecturerId: {lecturerId}");
                    TempData["Success"] = "Gán giảng viên thành công.";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveLecturer(int subjectId, int lecturerId)
        {
            var user = await GetCurrentUser();
            if (user == null) return Unauthorized();

            var assignment = await _context.SubjectAssignments
                .FirstOrDefaultAsync(sa => sa.SubjectId == subjectId && sa.LecturerId == lecturerId);

            if (assignment != null)
            {
                _context.SubjectAssignments.Remove(assignment);
                await _context.SaveChangesAsync();
                await _auditLogService.LogAsync(user.Id, "Remove Lecturer", subjectId.ToString(), $"LecturerId: {lecturerId}");
                TempData["Success"] = "Hủy gán giảng viên thành công.";
            }

            return RedirectToAction("Index");
        }
    }
}
