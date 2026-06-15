using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.PresentationRazorPage.Hubs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SubjectsModel : PageModel
    {
        private readonly IAuditLogService _auditLogService;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;
        private readonly IHubContext<AppNotificationHub> _hubContext;

        public SubjectsModel(
            IAuditLogService auditLogService,
            RagChatbot.DataAccess.Data.ApplicationDbContext context,
            IHubContext<AppNotificationHub> hubContext)
        {
            _auditLogService = auditLogService;
            _context = context;
            _hubContext = hubContext;
        }

        public System.Collections.Generic.List<Subject> SubjectsList { get; set; }

        public async Task OnGetAsync()
        {
            SubjectsList = await _context.Subjects
                .Include(s => s.Department)
                .ThenInclude(d => d.Users)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
            
            ViewData["Departments"] = await _context.Departments.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateSubjectAsync(string code, string name, int departmentId)
        {
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name) && departmentId > 0)
            {
                var exists = await _context.Subjects.AnyAsync(s => s.Code == code);
                if (exists)
                {
                    TempData["Error"] = "Mã môn học đã tồn tại.";
                    return RedirectToPage();
                }

                var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var subject = new Subject
                {
                    Code = code,
                    Name = name,
                    DepartmentId = departmentId,
                    IsActive = true
                };
                
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                
                await _auditLogService.LogAsync(adminId, "Create Subject", subject.Id.ToString(), $"Code: {code}, Name: {name}, DeptId: {departmentId}");
                await _hubContext.Clients.All.SendAsync("SubjectListChanged");
                TempData["Success"] = "Tạo môn học thành công.";
            }
            else
            {
                TempData["Error"] = "Vui lòng nhập đủ thông tin.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateSubjectsBulkAsync(Microsoft.AspNetCore.Http.IFormFile file, int departmentId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file chứa dữ liệu môn học.";
                return RedirectToPage();
            }
            if (departmentId <= 0)
            {
                TempData["Error"] = "Vui lòng chọn bộ môn hợp lệ.";
                return RedirectToPage();
            }

            using var reader = new System.IO.StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int successCount = 0;
            int failCount = 0;
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

            foreach (var line in lines)
            {
                var parts = line.Split(',', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    failCount++;
                    continue;
                }

                var code = parts[0].Trim();
                var name = parts[1].Trim();

                var exists = await _context.Subjects.AnyAsync(s => s.Code == code);
                if (exists)
                {
                    failCount++;
                    continue;
                }

                var subject = new Subject
                {
                    Code = code,
                    Name = name,
                    DepartmentId = departmentId,
                    IsActive = true
                };
                
                _context.Subjects.Add(subject);
                successCount++;
            }

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(adminId, "Bulk Create Subjects", "", $"Created {successCount} subjects");
            await _hubContext.Clients.All.SendAsync("SubjectListChanged");

            if (failCount > 0)
            {
                TempData["Success"] = $"Tạo thành công {successCount} môn học. Bỏ qua {failCount} dòng do sai định dạng hoặc trùng CODE.";
            }
            else
            {
                TempData["Success"] = $"Đã tạo thành công {successCount} môn học.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Department)
                .ThenInclude(d => d.Users)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (subject == null)
            {
                TempData["Error"] = "Môn học không tồn tại.";
                return RedirectToPage();
            }

            var hasManager = subject.Department?.Users?.Any(u => u.Role == "HeadOfDepartment") ?? false;
            if (subject.IsActive && hasManager)
            {
                TempData["Error"] = "Không thể vô hiệu hóa môn học đang có Trưởng bộ môn quản lý.";
                return RedirectToPage();
            }

            if (!subject.IsActive && subject.Department != null && !subject.Department.IsActive)
            {
                TempData["Error"] = "Không thể kích hoạt môn học vì bộ môn của nó đang bị vô hiệu hóa.";
                return RedirectToPage();
            }

            subject.IsActive = !subject.IsActive;
            await _context.SaveChangesAsync();
            
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Toggle Subject Active", subject.Id.ToString(), $"IsActive changed to {subject.IsActive}");
            await _hubContext.Clients.All.SendAsync("SubjectListChanged");

            TempData["Success"] = subject.IsActive ? "Đã kích hoạt môn học." : "Đã vô hiệu hóa môn học.";
            return RedirectToPage();
        }

    }
}
