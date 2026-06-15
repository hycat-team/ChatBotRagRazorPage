using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.PresentationRazorPage.Hubs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsModel : PageModel
    {
        private readonly IAuditLogService _auditLogService;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;
        private readonly IHubContext<AppNotificationHub> _hubContext;

        public DepartmentsModel(
            IAuditLogService auditLogService,
            RagChatbot.DataAccess.Data.ApplicationDbContext context,
            IHubContext<AppNotificationHub> hubContext)
        {
            _auditLogService = auditLogService;
            _context = context;
            _hubContext = hubContext;
        }

        public System.Collections.Generic.List<Department> DepartmentsList { get; set; }

        public void OnGet()
        {
            DepartmentsList = _context.Departments.OrderByDescending(d => d.Id).ToList();
        }

        public async Task<IActionResult> OnPostCreateDepartmentAsync(string name, string description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var dept = new Department { Name = name, Description = description, IsActive = true };
                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Create Department", dept.Id.ToString(), $"Name: {name}");

                TempData["Success"] = "Tạo Bộ môn thành công.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteDepartmentAsync(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                // Check if any HOD is currently managing this department
                bool isManaged = await _context.AppUsers.AnyAsync(u => u.DepartmentId == id && u.Role == "HeadOfDepartment");
                if (isManaged)
                {
                    TempData["Error"] = "Không thể xóa bộ môn này vì đang có Trưởng bộ môn quản lý.";
                    return RedirectToPage();
                }

                _context.Departments.Remove(dept);
                await _context.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Delete Department", dept.Id.ToString(), $"Name: {dept.Name}");

                TempData["Success"] = "Xóa bộ môn thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy bộ môn.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id)
        {
            // var dept = await _context.Departments.FindAsync(id);
            // if (dept == null)
            // {
            //     TempData["Error"] = "Không tìm thấy bộ môn.";
            //     return RedirectToPage();
            // }

            // if (dept.IsActive)
            // {
            //     bool isManaged = await _context.AppUsers.AnyAsync(u => u.DepartmentId == id && u.Role == "HeadOfDepartment");
            //     if (isManaged)
            //     {
            //         TempData["Error"] = "Không thể vô hiệu hóa bộ môn đang có Trưởng bộ môn quản lý.";
            //         return RedirectToPage();
            //     }
            // }

            // dept.IsActive = !dept.IsActive;

            // if (!dept.IsActive)
            // {
            //     var subjects = await _context.Subjects.Where(s => s.DepartmentId == id && s.IsActive).ToListAsync();
            //     foreach (var s in subjects)
            //     {
            //         s.IsActive = false;
            //     }
            //     _context.Subjects.UpdateRange(subjects);
            // }

            // await _context.SaveChangesAsync();

            // var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            // await _auditLogService.LogAsync(adminId, "Toggle Department Active", dept.Id.ToString(), $"IsActive changed to {dept.IsActive}");

            // // Notify clients since toggling department active status may cascade to subjects
            // await _hubContext.Clients.All.SendAsync("SubjectListChanged");

            // TempData["Success"] = dept.IsActive ? "Đã kích hoạt bộ môn." : "Đã vô hiệu hóa bộ môn (và các môn học thuộc bộ môn).";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateDepartmentAsync(int id, string name, string description)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null && !string.IsNullOrWhiteSpace(name))
            {
                dept.Name = name;
                dept.Description = description;
                _context.Departments.Update(dept);
                await _context.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Update Department", dept.Id.ToString(), $"Name: {name}");

                TempData["Success"] = "Cập nhật bộ môn thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy bộ môn hoặc thông tin không hợp lệ.";
            }
            return RedirectToPage();
        }
        public async Task<IActionResult> OnGetDepartmentTermHistoryAsync(int deptId)
        {
            var terms = await _context.HodTerms
                .Include(t => t.AppUser)
                .Where(t => t.DepartmentId == deptId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            var result = terms.Select(t => new {
                hodName = t.AppUser != null ? (t.AppUser.LastName + " " + t.AppUser.FirstName) : "Không rõ",
                startAt = t.StartAt.ToString("dd/MM/yyyy"),
                endAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
            });

            return new JsonResult(result);
        }
    }
}


