using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.Interfaces;
using RagChatbot.Business.Mappings;
using RagChatbot.Business.Services;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChatService _chatService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public DocumentController(
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

        [Authorize(Roles = "Admin,Lecturer,HeadOfDepartment,Student")]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");
            var isHod = User.IsInRole("HeadOfDepartment");
            var isLecturer = User.IsInRole("Lecturer");
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
            else if (isLecturer)
            {
                subjectsQuery = subjectsQuery.Where(s => s.Assignments.Any(a => a.LecturerId == userId));
            }
            else
            {
                // Phòng hờ trường hợp Role lạ khác không có quyền
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
            if (Request.Cookies.TryGetValue("LastUploadedSubjectId", out string? cookieVal) && int.TryParse(cookieVal, out int lastId))
            {
                lastSelectedSubjectId = lastId;
            }

            var viewModel = new RagChatbot.Presentation.ViewModels.DocumentIndexViewModel
            {
                Documents = documents,
                Subjects = subjects.Select(s => s.ToDto(true)!).ToList(),
                LastSelectedSubjectId = lastSelectedSubjectId
            };
            return View(viewModel);
        }

        [Authorize(Roles = "HeadOfDepartment,Lecturer")]
        [HttpPost]
        public async Task<IActionResult> Upload(
            int subjectId,
            List<IFormFile> files,
            [FromServices] IGoogleDriveService driveService,
            [FromServices] ILocalStorageService localStorage)
        {
            var userId = GetCurrentUserId();
            var subject = await _subjectService.GetByIdAsync(subjectId);
            
            if (subject == null)
            {
                TempData["Error"] = "Invalid subject.";
                return RedirectToAction(nameof(Index));
            }

            var isHod = User.IsInRole("HeadOfDepartment");
            if (isHod)
            {
                var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
                if (subject.DepartmentId != hodUser?.DepartmentId)
                {
                    TempData["Error"] = "Môn học này không thuộc bộ môn của bạn.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                var isAssigned = _context.SubjectAssignments.Any(sa => sa.SubjectId == subjectId && sa.LecturerId == userId);
                if (!isAssigned)
                {
                    TempData["Error"] = "Bạn chưa được gán để quản lý môn học này.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (files == null || files.Count == 0)
            {
                TempData["Error"] = "Please select valid files.";
                return RedirectToAction(nameof(Index));
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
                    Status = "Pending", // Sửa từ Processing thành Pending
                    UploadedAt = DateTime.UtcNow,
                    UploaderId = userId
                };

                await _documentService.AddAsync(document);
                await _auditLogService.LogAsync(userId, "Upload Document", "", $"File: {file.FileName} for SubjectId: {subjectId}");

                storageInfos.Add(storageInfo);
                successCount++;
            }

            // Save the last uploaded subjectId to Cookie
            Response.Cookies.Append("LastUploadedSubjectId", subjectId.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30) });

            if (failedFiles.Any())
            {
                TempData["Error"] = $"Uploaded {successCount} files. Failed files: {string.Join(", ", failedFiles)}";
            }
            else
            {
                var storageMsg = string.Join(" and ", storageInfos);
                TempData["Success"] = $"Successfully uploaded {successCount} files to {storageMsg} and pending processing.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "HeadOfDepartment")]
        [HttpPost]
        public async Task<IActionResult> CreateSubject(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Code and Name are required.";
                return RedirectToAction(nameof(Index));
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
                
                await _subjectService.AddAsync(new RagChatbot.Business.DTOs.CreateSubjectDto { Code = code.Trim(), Name = name.Trim(), UserId = userId, DepartmentId = deptId });
                TempData["Success"] = "Subject created.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Mã môn học này đã tồn tại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─── Quản lý Subject ─────────────────────────────────────────────────

        [Authorize(Roles = "HeadOfDepartment")]
        [HttpPost]
        public async Task<IActionResult> RenameSubject(int id, string name)
        {
            var subject = await _subjectService.GetByIdAsync(id);
            if (subject == null) return Json(new { success = false, message = "Subject not found." });

            subject.Name = name.Trim();
            await _subjectService.UpdateAsync(subject);
            return Json(new { success = true, name = subject.Name });
        }

        [Authorize(Roles = "HeadOfDepartment")]
        [HttpPost]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _subjectService.GetByIdAsync(id);

            if (subject == null) return Json(new { success = false, message = "Subject not found." });

            // Note: EF Core cascade delete will handle related documents, chunks, sessions, and messages if configured properly.
            // If manual cascade is required, it should be implemented in the SubjectService.
            await _subjectService.DeleteAsync(id);
            return Json(new { success = true });
        }

        // ─── Quản lý Document ────────────────────────────────────────────────

        [Authorize(Roles = "Admin,HeadOfDepartment,Lecturer")]
        [HttpPost]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);

            if (document == null) return Json(new { success = false, message = "Document not found." });
            
            if (document.UploaderId != userId && !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa tài liệu của người khác." });
            }

            await _documentService.DeleteAsync(id);
            await _auditLogService.LogAsync(userId, "Delete Document", id.ToString(), $"FileId: {id}");
            return Json(new { success = true });
        }

        [Authorize(Roles = "Admin,HeadOfDepartment,Lecturer")]
        [HttpPost]
        public async Task<IActionResult> ToggleDocumentActive(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);

            if (document == null) return Json(new { success = false, message = "Document not found." });
            
            bool canToggle = document.UploaderId == userId || User.IsInRole("Admin");
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
                return Json(new { success = false, message = "Bạn không có quyền sửa tài liệu này." });
            }

            document.IsActive = !document.IsActive;
            await _documentService.UpdateAsync(document);
            return Json(new { success = true, isActive = document.IsActive });
        }

        [Authorize(Roles = "Admin,HeadOfDepartment,Lecturer")]
        [HttpPost]
        public async Task<IActionResult> RenameDocument(int id, string displayName)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);

            if (document == null) return Json(new { success = false, message = "Document not found." });
            
            if (document.UploaderId != userId && !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "Bạn không có quyền đổi tên tài liệu của người khác." });
            }

            document.DisplayName = displayName.Trim();
            await _documentService.UpdateAsync(document);
            return Json(new { success = true, displayName = document.DisplayName });
        }

        // ─── API cho Chat Page ────────────────────────────────────────────────

        /// <summary>Trả về danh sách tài liệu đã Indexed của 1 subject (dùng cho Document Filter panel)</summary>
        [HttpGet]
        public async Task<IActionResult> GetSubjectDocuments(int subjectId)
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

            return Json(indexedDocs);
        }

        [Authorize(Roles = "Admin,HeadOfDepartment,Lecturer")]
        [HttpGet]
        public async Task<IActionResult> GetDocumentChunks(int id, [FromServices] IDocumentChunkRepository chunkRepository)
        {
            var userId = GetCurrentUserId();
            var document = await _documentService.GetByIdAsync(id);
            if (document == null)
            {
                return Json(new { success = false, message = "Tài liệu không tồn tại." });
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
                return Json(new { success = false, message = "Bạn không có quyền xem tài liệu này." });
            }

            var chunks = await chunkRepository.FindAsync(c => c.DocumentId == id);
            var result = chunks.Select(c => new { 
                id = c.Id, 
                content = c.Content, 
                pageNumber = c.PageNumber 
            }).OrderBy(c => c.pageNumber).ThenBy(c => c.id).ToList();

            return Json(new { success = true, chunks = result });
        }

        // ─── Quản lý Giảng viên môn học (Cho Trưởng bộ môn) ────────────────────────────────────────────────

        [Authorize(Roles = "HeadOfDepartment")]
        [HttpGet]
        public async Task<IActionResult> GetSubjectAssignments(int subjectId)
        {
            var userId = GetCurrentUserId();
            var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
            var subject = await _subjectService.GetByIdAsync(subjectId);
            
            if (subject == null || subject.DepartmentId != hodUser?.DepartmentId)
            {
                return Json(new { success = false, message = "Bạn không có quyền quản lý môn học này." });
            }

            var departmentLecturers = _context.AppUsers
                .Where(u => u.Role == "Lecturer" && u.DepartmentId == hodUser.DepartmentId)
                .Select(u => new { id = u.Id, name = $"{u.LastName} {u.FirstName}", email = u.Email })
                .ToList();

            var assignedLecturerIds = _context.SubjectAssignments
                .Where(sa => sa.SubjectId == subjectId)
                .Select(sa => sa.LecturerId)
                .ToList();

            return Json(new { success = true, lecturers = departmentLecturers, assignedIds = assignedLecturerIds });
        }

        [Authorize(Roles = "HeadOfDepartment")]
        [HttpPost]
        public async Task<IActionResult> UpdateSubjectAssignments(int subjectId, [FromBody] List<int> lecturerIds)
        {
            var userId = GetCurrentUserId();
            var hodUser = _context.AppUsers.FirstOrDefault(u => u.Id == userId);
            var subject = await _subjectService.GetByIdAsync(subjectId);
            
            if (subject == null || subject.DepartmentId != hodUser?.DepartmentId)
            {
                return Json(new { success = false, message = "Bạn không có quyền quản lý môn học này." });
            }

            var existingAssignments = _context.SubjectAssignments.Where(sa => sa.SubjectId == subjectId);
            _context.SubjectAssignments.RemoveRange(existingAssignments);

            if (lecturerIds != null && lecturerIds.Any())
            {
                var newAssignments = lecturerIds.Select(id => new RagChatbot.DataAccess.EntityModels.SubjectAssignment
                {
                    SubjectId = subjectId,
                    LecturerId = id
                });
                _context.SubjectAssignments.AddRange(newAssignments);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> Browse(int? subjectId, string searchString)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isPremium = false;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _context.AppUsers.FindAsync(userId);
                if (user != null)
                {
                    isPremium = user.Subscription == AppUser.SubscriptionType.Premium;
                }
            }
            ViewBag.IsPremium = isPremium; 
                                           
            // 1. Lấy danh sách tất cả môn học để học sinh chọn lọc
            var subjects = _context.Subjects.OrderBy(s => s.Name).ToList();

            // 2. Tạo câu truy vấn: Lấy file kèm theo thông tin môn học (.Include)
            var query = _context.Documents
                .Include(d => d.Subject)
                .Where(d => d.Status == "Indexed" && d.IsActive)
                .AsQueryable();

            // 3. Logic lọc theo môn học khi học sinh chọn dropdown
            if (subjectId.HasValue && subjectId.Value > 0)
            {
                query = query.Where(d => d.SubjectId == subjectId.Value);
            }

            // 4. Logic tìm kiếm theo từ khóa học sinh gõ
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                string keyword = searchString.Trim().ToLower();
                query = query.Where(d => d.FileName.ToLower().Contains(keyword) ||
                                     (d.DisplayName != null && d.DisplayName.ToLower().Contains(keyword)));
            }

            // 5. Thực thi lấy dữ liệu
            var documents = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();

            // 6. Đẩy dữ liệu ra giao diện Browse.cshtml
            ViewBag.Subjects = subjects;
            ViewBag.SelectedSubjectId = subjectId;
            ViewBag.SearchString = searchString;

            return View(documents);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Lecturer,HeadOfDepartment,Student")]
        public async Task<IActionResult> ViewDocument(int id)
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
                        return RedirectToAction("Browse"); 
                    }
                }
            }

            // 1. Tìm thông tin tài liệu trong DB (Chuyển sang dùng bản Async luôn cho mượt)
            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
            if (document == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin tài liệu trên hệ thống.";
                return RedirectToAction("Dashboard");
            }

            string rawPath = document.FilePath;
            if (string.IsNullOrEmpty(rawPath))
            {
                TempData["Error"] = "Tài liệu này không có thông tin FilePath trong cơ sở dữ liệu.";
                return RedirectToAction("Dashboard");
            }

            // 2. Bóc tách lấy tên file trần sạch sẽ (Giữ nguyên logic của ông)
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

            // 3. TẠO DANH SÁCH BAO VÂY TẤT CẢ CÁC NƠI FILE CÓ THỂ TRỐN (Giữ nguyên logic Docker)
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
                return RedirectToAction("Dashboard");
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
