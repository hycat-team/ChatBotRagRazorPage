using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.PresentationRazorPage.Hubs;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SubjectsModel : PageModel
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ISubjectService _subjectService;
        private readonly IDepartmentService _departmentService;
        private readonly IHubContext<AppNotificationHub> _hubContext;

        public SubjectsModel(
            IAuditLogService auditLogService,
            ISubjectService subjectService,
            IDepartmentService departmentService,
            IHubContext<AppNotificationHub> hubContext)
        {
            _auditLogService = auditLogService;
            _subjectService = subjectService;
            _departmentService = departmentService;
            _hubContext = hubContext;
        }

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.SubjectDto> SubjectsList { get; set; }

        public async Task OnGetAsync()
        {
            SubjectsList = await _subjectService.GetAllAsync();

            ViewData["Departments"] = await _departmentService.GetAllDepartmentsAsync();
        }

        public async Task<IActionResult> OnPostCreateSubjectAsync(string code, string name, int departmentId)
        {
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name) && departmentId > 0)
            {
                var exists = await _subjectService.ExistsByCodeAsync(code);
                if (exists)
                {
                    TempData["Error"] = "Mã môn học đã tồn tại.";
                    return RedirectToPage();
                }

                var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var subjectDto = new RagChatbot.Business.DTOs.CreateSubjectDto
                {
                    Code = code,
                    Name = name,
                    DepartmentId = departmentId
                };

                var createdSubject = await _subjectService.AddAsync(subjectDto);

                await _auditLogService.LogAsync(adminId, "Create Subject", createdSubject.Id.ToString(), $"Code: {code}, Name: {name}, DeptId: {departmentId}");
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

                var exists = await _subjectService.ExistsByCodeAsync(code);
                if (exists)
                {
                    failCount++;
                    continue;
                }

                var subjectDto = new RagChatbot.Business.DTOs.CreateSubjectDto
                {
                    Code = code,
                    Name = name,
                    DepartmentId = departmentId
                };

                await _subjectService.AddAsync(subjectDto);
                successCount++;
            }

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

        public async Task<IActionResult> OnPostUpdateSubjectAsync(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vui lòng nhập tên môn học.";
                return RedirectToPage();
            }

            var subject = await _subjectService.GetByIdAsync(id);
            if (subject == null)
            {
                TempData["Error"] = "Môn học không tồn tại.";
                return RedirectToPage();
            }

            subject.Name = name;
            await _subjectService.UpdateAsync(subject);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Update Subject", subject.Id.ToString(), $"Name: {name}");
            await _hubContext.Clients.All.SendAsync("SubjectListChanged");

            TempData["Success"] = "Cập nhật môn học thành công.";
            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var subject = await _subjectService.GetByIdAsync(id);
            if (subject == null)
            {
                TempData["Error"] = "Môn học không tồn tại.";
                return RedirectToPage();
            }

            // Thực hiện đảo trạng thái ẩn/hiện
            await _subjectService.ToggleStatusAsync(id);

            // Ghi log hệ thống và đồng bộ SignalR
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            string logAction = subject.IsActive ? "Ẩn môn học" : "Kích hoạt lại môn học";
            await _auditLogService.LogAsync(adminId, logAction, id.ToString(), $"Môn học: {subject.Name} ({subject.Code})");

            await _hubContext.Clients.All.SendAsync("SubjectListChanged");

            TempData["Success"] = $"{(subject.IsActive ? "Ẩn" : "Kích hoạt")} môn học thành công.";
            return RedirectToPage();
        }

    }
}
