using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.Business.Interfaces;
using RagChatbot.Business.Mappings;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Document
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChatService _chatService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChatService chatService,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            RagChatbot.DataAccess.Data.ApplicationDbContext context,
            IAuditLogService auditLogService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chatService = chatService;
            _env = env;
            _context = context;
            _auditLogService = auditLogService;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out int userId) ? userId : 0;
        }

        public RagChatbot.PresentationRazorPage.ViewModels.DocumentIndexViewModel ViewModel { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            var isHod = User.IsInRole("HeadOfDepartment");
            var isStudent = User.IsInRole("Student");

            var subjectsQuery = _context.Subjects.Include(s => s.Documents).AsQueryable();

            if (isAdmin || isStudent)
            {
                // Admin sees all subjects, Student thay moi tai lieu
            }
            else if (isHod)
            {
                var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                if (hodUser != null && hodUser.DepartmentId != null)
                {
                    subjectsQuery = subjectsQuery.Where(s => s.DepartmentId == hodUser.DepartmentId);
                }
                else 
                {
                    subjectsQuery = subjectsQuery.Where(s => false);
                }
            }
            else
            {
                subjectsQuery = subjectsQuery.Where(s => false);
            }

            var subjects = subjectsQuery.ToList();
            var subjectIds = subjects.Select(s => s.Id).ToList();
            
            var documents = new List<RagChatbot.Business.DTOs.DocumentDto>();
            
            foreach (var subjectId in subjectIds)
            {
                var docs = await _documentService.GetBySubjectIdAsync(subjectId);
                documents.AddRange(docs);
            }
            
            int? lastSelectedSubjectId = null;
            if (Request.Cookies.TryGetValue("LastUploadedSubjectId", out string cookieVal) && int.TryParse(cookieVal, out int lastId))
            {
                lastSelectedSubjectId = lastId;
            }

            ViewModel = new RagChatbot.PresentationRazorPage.ViewModels.DocumentIndexViewModel
            {
                Documents = documents,
                Subjects = subjects.Select(s => s.ToDto(true)!).ToList(),
                LastSelectedSubjectId = lastSelectedSubjectId
            };
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(
            int subjectId,
            List<IFormFile> files,
            [FromServices] IGoogleDriveService driveService,
            [FromServices] ILocalStorageService localStorage,
            [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<RagChatbot.PresentationRazorPage.Hubs.AppNotificationHub> hubContext)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectService.GetByIdAsync(subjectId);
            
            if (subject == null)
            {
                if (Request.Headers["Accept"].ToString().Contains("application/json")) return new JsonResult(new { success = false, message = "Invalid subject." });
                TempData["Error"] = "Invalid subject.";
                return RedirectToPage();
            }

            var isHod = User.IsInRole("HeadOfDepartment");
            if (isHod)
            {
                var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                if (subject.DepartmentId != hodUser?.DepartmentId)
                {
                    if (Request.Headers["Accept"].ToString().Contains("application/json")) return new JsonResult(new { success = false, message = "Môn học này không thuộc bộ môn của bạn." });
                    TempData["Error"] = "Môn học này không thuộc bộ môn của bạn.";
                    return RedirectToPage();
                }
            }

            if (files == null || files.Count == 0)
            {
                if (Request.Headers["Accept"].ToString().Contains("application/json")) return new JsonResult(new { success = false, message = "Please select valid files." });
                TempData["Error"] = "Please select valid files.";
                return RedirectToPage();
            }

            var successCount = 0;
            var failedFiles = new List<string>();
            var storageInfos = new HashSet<string>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                string filePath;
                string storageInfo;

                try
                {
                    using var stream = file.OpenReadStream();
                    filePath = await driveService.UploadFileAsync(stream, file.FileName, file.ContentType);
                    storageInfo = "Google Drive";
                }
                catch (Exception driveEx)
                {
                    try
                    {
                        using var stream = file.OpenReadStream();
                        filePath = await localStorage.SaveFileAsync(stream, file.FileName);
                        storageInfo = "local server";
                    }
                    catch (Exception localEx)
                    {
                        failedFiles.Add($"{file.FileName} (Drive: {driveEx.Message} | Local: {localEx.Message})");
                        continue;
                    }
                }

                var document = new RagChatbot.Business.DTOs.CreateDocumentDto
                {
                    SubjectId = subjectId,
                    FileName = file.FileName,
                    FilePath = filePath,
                    IsActive = false,
                    Status = "Pending",
                    UploadedAt = DateTime.UtcNow,
                    UploaderId = userId
                };

                await _documentService.AddAsync(document);
                await _auditLogService.LogAsync(userId, "Upload Document", "", $"File: {file.FileName} for SubjectId: {subjectId}");

                storageInfos.Add(storageInfo);
                successCount++;
            }

            Response.Cookies.Append("LastUploadedSubjectId", subjectId.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            await hubContext.Clients.All.SendAsync("DocumentListChanged");

            if (failedFiles.Any())
            {
                var errMsg = $"Uploaded {successCount} files. Failed files: {string.Join(", ", failedFiles)}";
                if (Request.Headers["Accept"].ToString().Contains("application/json")) return new JsonResult(new { success = false, message = errMsg });
                TempData["Error"] = errMsg;
            }
            else
            {
                var storageMsg = string.Join(" and ", storageInfos);
                var successMsg = $"Successfully uploaded {successCount} files to {storageMsg} and pending processing.";
                if (Request.Headers["Accept"].ToString().Contains("application/json")) return new JsonResult(new { success = true, message = successMsg });
                TempData["Success"] = successMsg;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateSubjectAsync(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Code and Name are required.";
                return RedirectToPage();
            }

            var userId = GetCurrentUserId();
            try
            {
                var isHod = User.IsInRole("HeadOfDepartment");
                int? deptId = null;
                if (isHod)
                {
                    var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                    deptId = hodUser?.DepartmentId;
                }
                
                await _subjectService.AddAsync(new RagChatbot.Business.DTOs.CreateSubjectDto { Code = code.Trim(), Name = name.Trim(), DepartmentId = deptId });
                TempData["Success"] = "Subject created.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Mã môn học này đã tồn tại.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRenameSubjectAsync(int id, string name)
        {
            var subject = await _subjectService.GetByIdAsync(id);
            if (subject == null) return new JsonResult(new { success = false, message = "Subject not found." });

            subject.Name = name.Trim();
            await _subjectService.UpdateAsync(subject);
            return new JsonResult(new { success = true, name = subject.Name });
        }

        public async Task<IActionResult> OnPostDeleteSubjectAsync(int id)
        {
            var subject = await _subjectService.GetByIdAsync(id);

            if (subject == null) return new JsonResult(new { success = false, message = "Subject not found." });

            await _subjectService.DeleteAsync(id);
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteDocumentAsync(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);

            if (document == null) return new JsonResult(new { success = false, message = "Document not found." });
            
            var isHod = User.IsInRole("HeadOfDepartment");
            bool canDelete = User.IsInRole("Admin");
            if (isHod && !canDelete)
            {
                var subject = await _subjectService.GetByIdAsync(document.SubjectId);
                var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                if (subject != null && subject.DepartmentId == hodUser?.DepartmentId)
                {
                    canDelete = true;
                }
            }
            if (!canDelete)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xóa tài liệu này." });
            }

            await _documentService.DeleteAsync(id);
            await _auditLogService.LogAsync(userId, "Delete Document", id.ToString(), $"FileId: {id}");
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostToggleDocumentActiveAsync(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);

            if (document == null) return new JsonResult(new { success = false, message = "Document not found." });
            
            bool canToggle = User.IsInRole("Admin");
            if (!canToggle && User.IsInRole("HeadOfDepartment"))
            {
                var subject = await _subjectService.GetByIdAsync(document.SubjectId);
                var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                if (subject != null && subject.DepartmentId == hodUser?.DepartmentId)
                {
                    canToggle = true;
                }
            }

            if (!canToggle)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền sửa tài liệu này." });
            }

            document.IsActive = !document.IsActive;
            await _documentService.UpdateAsync(document);
            return new JsonResult(new { success = true, isActive = document.IsActive });
        }

        public async Task<IActionResult> OnPostRenameDocumentAsync(int id, string displayName)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);

            if (document == null) return new JsonResult(new { success = false, message = "Document not found." });
            
            var isHod = User.IsInRole("HeadOfDepartment");
            bool canRename = User.IsInRole("Admin");
            if (isHod && !canRename)
            {
                var subject = await _subjectService.GetByIdAsync(document.SubjectId);
                var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                if (subject != null && subject.DepartmentId == hodUser?.DepartmentId)
                {
                    canRename = true;
                }
            }
            if (!canRename)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền đổi tên tài liệu này." });
            }

            document.DisplayName = displayName.Trim();
            await _documentService.UpdateAsync(document);
            return new JsonResult(new { success = true, displayName = document.DisplayName });
        }

        public async Task<IActionResult> OnGetSubjectDocumentsAsync(int subjectId)
        {
            var docs = await _documentService.GetBySubjectIdAsync(subjectId);
            var indexedDocs = docs
                .Where(d => d.Status == "Indexed" && d.IsActive)
                .Select(d => new { 
                    d.Id, 
                    FileName = string.IsNullOrWhiteSpace(d.DisplayName) ? d.FileName : d.DisplayName, 
                    d.UploadedAt, 
                    d.UploaderFullName 
                })
                .OrderBy(d => d.FileName)
                .ToList();

            return new JsonResult(indexedDocs);
        }

        public async Task<IActionResult> OnGetDocumentChunksAsync(int id, [FromServices] IDocumentChunkRepository chunkRepository)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);
            if (document == null)
            {
                return new JsonResult(new { success = false, message = "Tài liệu không tồn tại." });
            }
            
            bool canView = document.UploaderId == userId || User.IsInRole("Admin");
            if (!canView && User.IsInRole("HeadOfDepartment"))
            {
                var subject = await _subjectService.GetByIdAsync(document.SubjectId);
                var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                if (subject != null && subject.DepartmentId == hodUser?.DepartmentId)
                {
                    canView = true;
                }
            }

            if (!canView)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xem tài liệu này." });
            }

            var chunks = await chunkRepository.FindAsync(c => c.DocumentId == id);
            var result = chunks.Select(c => new { 
                id = c.Id, 
                content = c.Content, 
                pageNumber = c.PageNumber 
            }).OrderBy(c => c.pageNumber).ThenBy(c => c.id).ToList();

            return new JsonResult(new { success = true, chunks = result });
        }

        public async Task<IActionResult> OnGetViewDocumentAsync(int id)
        {
            if (User.IsInRole("Student"))
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var user = await _context.AppUsers.FindAsync(userId);
                    if (user != null && user.Subscription == AppUser.SubscriptionType.Free)
                    {
                        TempData["Error"] = "Tính năng đọc tài liệu chỉ dành riêng cho tài khoản Premium. Vui lòng nâng cấp gói để mở khóa!";
                        return RedirectToPage("/Document/Browse"); 
                    }
                }
            }

            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
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
                Path.Combine(webRoot, fileNameOnDisk);
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


