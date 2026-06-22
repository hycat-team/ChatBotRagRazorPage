using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using RagChatbot.Business.Interfaces;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IAppUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly IDepartmentService _departmentService;
        private readonly IContactService _contactService;
        private readonly IAuditLogService _auditLogService;
        private readonly IWebHostEnvironment _env;

        public DashboardModel(
            IAppUserService userService,
            IDocumentService documentService,
            IDepartmentService departmentService,
            IContactService contactService,
            IAuditLogService auditLogService,
            IWebHostEnvironment env)
        {
            _userService = userService;
            _documentService = documentService;
            _departmentService = departmentService;
            _contactService = contactService;
            _auditLogService = auditLogService;
            _env = env;
        }

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.DocumentDto> Documents { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Documents = await _documentService.GetRecentDocumentsAsync(10);

            var uploaders = await _userService.GetUsersByRoleAsync("HeadOfDepartment");
            var departments = await _departmentService.GetAllDepartmentsAsync();

            ViewData["ActiveCount"] = await _documentService.GetActiveCountAsync();
            ViewData["ProcessingCount"] = await _documentService.GetProcessingCountAsync();

            int premiumUsersCount = await _userService.GetPremiumUsersCountAsync();

            long packagePrice = 100000;
            long totalRevenue = premiumUsersCount * packagePrice;

            ViewData["PremiumCount"] = premiumUsersCount;
            ViewData["TotalRevenue"] = totalRevenue;

            ViewData["PendingContactsCount"] = await _contactService.GetPendingCountAsync();

            ViewData["Uploaders"] = uploaders.ToList();
            ViewData["Departments"] = departments.ToList();
        }

        public async Task<IActionResult> OnPostDeleteDocumentFromDashboardAsync(int id)
        {
            var doc = await _documentService.GetByIdAsync(id);
            if (doc != null)
            {
                await _documentService.DeleteAsync(id);

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Admin Delete Document From Dashboard", id.ToString(), $"Document Deleted");

                TempData["Success"] = "Đã gỡ bỏ tài liệu học liệu thành công khỏi hệ thống.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài liệu này.";
            }
            return RedirectToPage();
        }

        // [Authorize(Roles = "Admin,HeadOfDepartment,Student")] // MVC1001: Cannot be applied to handler methods
        public async Task<IActionResult> OnGetViewDocumentAsync(int id)
        {
            var document = await _documentService.GetByIdAsync(id);
            if (document == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin tài liệu trên hệ thống.";
                return RedirectToPage("/Admin/Dashboard");
            }

            string rawPath = document.FilePath;
            if (string.IsNullOrEmpty(rawPath))
            {
                TempData["Error"] = "Tài liệu này không có thông tin FilePath trong cơ sở dữ liệu.";
                return RedirectToPage("/Admin/Dashboard");
            }

            string fileNameOnDisk = rawPath;
            if (fileNameOnDisk.StartsWith("local://", StringComparison.OrdinalIgnoreCase))
            {
                fileNameOnDisk = fileNameOnDisk.Substring(8);
            }

            if (fileNameOnDisk.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) ||
                fileNameOnDisk.StartsWith("uploads\\", StringComparison.OrdinalIgnoreCase))
            {
                fileNameOnDisk = fileNameOnDisk.Substring(8);
            }

            string projectRoot = _env.ContentRootPath;
            string webRoot = _env.WebRootPath;

            var possiblePaths = new System.Collections.Generic.List<string>
            {
                Path.Combine(projectRoot, "uploads", fileNameOnDisk),
                Path.Combine(projectRoot, fileNameOnDisk)
            };

            if (!string.IsNullOrEmpty(webRoot))
            {
                possiblePaths.Add(Path.Combine(webRoot, "uploads", fileNameOnDisk));
                possiblePaths.Add(Path.Combine(webRoot, fileNameOnDisk));
            }
            string? absolutePath = null;
            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    absolutePath = path;
                    break;
                }
            }

            if (string.IsNullOrEmpty(absolutePath))
            {
                string searchedLocations = string.Join(" | ", possiblePaths);
                TempData["Error"] = $"Không tìm thấy file thực tế trên ổ đĩa! Code đã tìm kiếm kỹ tại các vị trí sau nhưng đều trống không: [{searchedLocations}]. Vui lòng kiểm tra lại file hoặc logic Upload.";
                return RedirectToPage("/Admin/Dashboard");
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(absolutePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            string downloadName = document.FileName ?? Path.GetFileName(absolutePath);
            return PhysicalFile(Path.GetFullPath(absolutePath), contentType, downloadName);
        }
    }
}

