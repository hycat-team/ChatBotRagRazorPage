using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DashboardModel(
            IAppUserRepository userRepository,
            IDocumentRepository documentRepository,
            IAuditLogService auditLogService,
            RagChatbot.DataAccess.Data.ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _userRepository = userRepository;
            _documentRepository = documentRepository;
            _auditLogService = auditLogService;
            _context = context;
            _env = env;
        }

        public System.Collections.Generic.List<RagChatbot.DataAccess.EntityModels.Document> Documents { get; set; }

        public void OnGet()
        {
            Documents = _context.Documents
                                    .Include(d => d.Subject)
                                    .OrderByDescending(d => d.UploadedAt)
                                    .Take(10)
                                    .ToList();

            var uploaders = _userRepository.Query()
                .Where(u => u.Role == "HeadOfDepartment")
                .ToList();

            var departments = _context.Departments.ToList();

            ViewData["ActiveCount"] = _context.Documents.Count(d => d.IsActive == true);
            ViewData["ProcessingCount"] = _context.Documents.Count(d => d.IsActive == false);

            int premiumUsersCount = _userRepository.Query().Count(u => u.Subscription == AppUser.SubscriptionType.Premium);

            long packagePrice = 100000;
            long totalRevenue = premiumUsersCount * packagePrice;

            ViewData["PremiumCount"] = premiumUsersCount;
            ViewData["TotalRevenue"] = totalRevenue;

            ViewData["PendingContactsCount"] = _context.ContactMessages.Count(c => c.Status == ContactStatus.Pending);

            ViewData["Uploaders"] = uploaders;
            ViewData["Departments"] = departments;
        }

        public async Task<IActionResult> OnPostDeleteDocumentFromDashboardAsync(int id)
        {
            var doc = await _documentRepository.GetByIdAsync(id);
            if (doc != null)
            {
                _documentRepository.Remove(doc);
                await _documentRepository.SaveChangesAsync();

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

        [Authorize(Roles = "Admin,HeadOfDepartment,Student")]
        public IActionResult OnGetViewDocument(int id)
        {
            var document = _context.Documents.FirstOrDefault(d => d.Id == id);
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
            string absolutePath = null;
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

