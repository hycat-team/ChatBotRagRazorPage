
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.PresentationRazorPage.Hubs;
using RagChatbot.Business.Interfaces;

using System.Security.Claims;


namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsModel : PageModel
    {
        private readonly IAuditLogService _auditLogService;
        private readonly IDepartmentService _departmentService;
        private readonly IAppUserService _userService;
        private readonly IHubContext<AppNotificationHub> _hubContext;

        public DepartmentsModel(
            IAuditLogService auditLogService,
            IDepartmentService departmentService,
            IAppUserService userService,
            IHubContext<AppNotificationHub> hubContext)
        {
            _auditLogService = auditLogService;
            _departmentService = departmentService;
            _userService = userService;
            _hubContext = hubContext;
        }

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.DepartmentDto> DepartmentsList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            DepartmentsList = await _departmentService.GetAllDepartmentsAsync();
        }

        public async Task<IActionResult> OnPostCreateDepartmentAsync(string name, string description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var deptDto = new RagChatbot.Business.DTOs.DepartmentDto { Name = name, Description = description, IsActive = true };
                var createdDept = await _departmentService.AddDepartmentAsync(deptDto);

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Create Department", createdDept.Id.ToString(), $"Name: {name}");

                TempData["Success"] = "Tạo Bộ môn thành công.";
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
            var dept = await _departmentService.GetByIdAsync(id);
            if (dept != null && !string.IsNullOrWhiteSpace(name))
            {
                dept.Name = name;
                dept.Description = description;
                await _departmentService.UpdateDepartmentAsync(dept);

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Update Department", id.ToString(), $"Name: {name}");

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
            var result = await _userService.GetDepartmentTermHistoryAsync(deptId);
            return new JsonResult(result.Select(t => new
            {
                hodName = t.HodName,
                startAt = t.StartAt,
                endAt = t.EndAt
            }));
        }
    }
}


