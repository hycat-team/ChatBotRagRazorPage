using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IDocumentRepository _documentRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(
            IAppUserRepository userRepository,
            IAuthService authService,
            IDocumentRepository documentRepository,
            IAuditLogService auditLogService,
            RagChatbot.DataAccess.Data.ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _userRepository = userRepository;
            _authService = authService;
            _documentRepository = documentRepository;
            _auditLogService = auditLogService;
            _context = context;
            _env = env; // Gán giá trị môi trường
        }

        public async Task<IActionResult> Index(string searchEmail = "")
        {
            var query = _userRepository.Query().Where(u => u.Role == "Student");

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(u => u.Email.Contains(searchEmail));
            }

            var users = query.ToList();

            ViewBag.Departments = _context.Departments.ToList();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUsers(Microsoft.AspNetCore.Http.IFormFile file, string role, int? departmentId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file chứa dữ liệu tài khoản.";
                return RedirectToAction("Index");
            }

            using var reader = new System.IO.StreamReader(file.OpenReadStream());
            var userData = await reader.ReadToEndAsync();
            var lines = userData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            int successCount = 0;
            int failCount = 0;
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var emailService = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailService)) as RagChatbot.Business.Interfaces.IEmailService;

            foreach (var line in lines)
            {
                var parts = line.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    failCount++;
                    continue; // Skip invalid lines
                }

                var email = parts[0].Trim();
                var lastName = parts[1].Trim();
                var firstName = string.Join(" ", parts.Skip(2)).Trim();

                // Generate default password
                var password = "123456";

                var success = await _authService.RegisterAsync(email, password, role, firstName, lastName);
                if (success)
                {


                    if (emailService != null)
                    {
                        var htmlBody = GetWelcomeEmailHtml(firstName, lastName, email, password);
                        await emailService.SendEmailAsync(
                            email,
                            "Thông tin tài khoản RAG Chatbot",
                            htmlBody
                        );
                    }

                    successCount++;
                    await _auditLogService.LogAsync(adminId, $"Create {role}", "", $"Email: {email}");
                }
                else
                {
                    failCount++;
                }
            }

            if (failCount > 0)
            {
                TempData["Error"] = $"Tạo thành công {successCount} tài khoản. Bỏ qua {failCount} tài khoản do trùng lặp Email đã tồn tại hoặc sai định dạng.";
            }
            else
            {
                TempData["Success"] = $"Đã tạo thành công toàn bộ {successCount} tài khoản.";
            }
            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user != null && user.Role != "Admin")
            {
                if (user.Role == "HeadOfDepartment" && user.DepartmentId.HasValue)
                {
                    var activeTerm = _context.HodTerms.FirstOrDefault(t => t.AppUserId == user.Id && t.EndAt == null);
                    if (activeTerm != null) activeTerm.EndAt = DateTime.UtcNow;
                    user.DepartmentId = null;
                }
                
                user.IsDeleted = true;
                user.IsActive = false;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Delete User", user.Id.ToString(), $"Email: {user.Email}");

                TempData["Success"] = "Xóa tài khoản thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản hoặc không được phép xóa.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null && user.Role != "Admin")
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var bytes = System.Text.Encoding.UTF8.GetBytes("123456");
                user.PasswordHash = System.Convert.ToBase64String(sha256.ComputeHash(bytes));
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Reset Password", user.Id.ToString(), $"Email: {user.Email}");

                TempData["Success"] = $"Đặt lại mật khẩu thành công cho tài khoản {user.Email} (Mật khẩu mới: 123456).";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản hoặc không được phép đổi mật khẩu.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EndHodTerm(int id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user != null && user.Role == "HeadOfDepartment" && user.DepartmentId.HasValue)
            {
                var activeTerm = _context.HodTerms.FirstOrDefault(t => t.AppUserId == user.Id && t.EndAt == null);
                if (activeTerm != null) activeTerm.EndAt = DateTime.UtcNow;
                
                user.DepartmentId = null;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();
                
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "End HOD Term", user.Id.ToString(), $"User: {user.Email}");
                
                TempData["Success"] = "Đã kết thúc nhiệm kỳ Trưởng bộ môn.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy Trưởng bộ môn hoặc không trong nhiệm kỳ.";
            }
            return RedirectToAction("HeadOfDepartments");
        }

        [HttpGet]
        public async Task<IActionResult> GetHodTermHistory(int userId)
        {
            var terms = await _context.HodTerms
                .Include(t => t.Department)
                .Where(t => t.AppUserId == userId)
                .OrderByDescending(t => t.StartAt)
                .Select(t => new {
                    departmentName = t.Department.Name,
                    startAt = t.StartAt.ToString("dd/MM/yyyy"),
                    endAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
                })
                .ToListAsync();
            return Json(terms);
        }

        // --- DEPARTMENT MANAGEMENT ---
        public IActionResult Departments()
        {
            var depts = _context.Departments.ToList();
            return View(depts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(string name, string description)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var dept = new RagChatbot.DataAccess.EntityModels.Department { Name = name, Description = description };
                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Create Department", dept.Id.ToString(), $"Name: {name}");

                TempData["Success"] = "Tạo Bộ môn thành công.";
            }
            return RedirectToAction("Departments");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
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
            return RedirectToAction("Departments");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDepartment(int id, string name, string description)
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
            return RedirectToAction("Departments");
        }

        public async Task<IActionResult> Subjects()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Department)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
            
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(subjects);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubject(string code, string name, int departmentId)
        {
            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name) && departmentId > 0)
            {
                var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var subject = new Subject
                {
                    Code = code,
                    Name = name,
                    DepartmentId = departmentId,
                    UserId = adminId
                };
                
                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();
                
                await _auditLogService.LogAsync(adminId, "Create Subject", subject.Id.ToString(), $"Code: {code}, Name: {name}, DeptId: {departmentId}");
                TempData["Success"] = "Tạo môn học thành công.";
            }
            else
            {
                TempData["Error"] = "Vui lòng nhập đủ thông tin.";
            }

            return RedirectToAction("Subjects");
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubjectsBulk(Microsoft.AspNetCore.Http.IFormFile file, int departmentId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file chứa dữ liệu môn học.";
                return RedirectToAction("Subjects");
            }
            if (departmentId <= 0)
            {
                TempData["Error"] = "Vui lòng chọn bộ môn hợp lệ.";
                return RedirectToAction("Subjects");
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
                    UserId = adminId
                };
                
                _context.Subjects.Add(subject);
                successCount++;
            }

            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(adminId, "Bulk Create Subjects", "", $"Created {successCount} subjects");

            if (failCount > 0)
            {
                TempData["Error"] = $"Tạo thành công {successCount} môn học. Bỏ qua {failCount} dòng do sai định dạng hoặc trùng CODE.";
            }
            else
            {
                TempData["Success"] = $"Đã tạo thành công {successCount} môn học.";
            }

            return RedirectToAction("Subjects");
        }

        [HttpPost]
        public async Task<IActionResult> CreateSingleUser(string email, string firstName, string lastName, string password, string role, int? departmentId)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập đủ thông tin.";
                return RedirectToAction("Index");
            }

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["Error"] = "Email đã tồn tại.";
                return RedirectToAction("Index");
            }

            var user = new AppUser
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = HashPassword(password),
                Role = role ?? "Student",
                DepartmentId = departmentId
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Create Single User", user.Id.ToString(), $"Email: {email}, Role: {user.Role}");

            var emailService = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailService)) as RagChatbot.Business.Interfaces.IEmailService;
            if (emailService != null)
            {
                var htmlBody = GetWelcomeEmailHtml(firstName, lastName, email, password);
                await emailService.SendEmailAsync(
                    email,
                    "Thông tin tài khoản RAG Chatbot",
                    htmlBody
                );
            }

            TempData["Success"] = "Tạo người dùng thành công.";
            return RedirectToAction("Index");
        }

        public IActionResult HeadOfDepartments()
        {
            var hods = _userRepository.Query()
                .Where(u => u.Role == "HeadOfDepartment")
                .ToList();

            // Populate department for view
            var depts = _context.Departments.ToList();
            foreach (var hod in hods)
            {
                if (hod.DepartmentId.HasValue)
                {
                    hod.Department = depts.FirstOrDefault(d => d.Id == hod.DepartmentId.Value);
                }
            }

            ViewBag.Departments = depts;
            return View(hods);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHod(string email, string firstName, string lastName, int departmentId)
        {
            var existingHod = _context.AppUsers.FirstOrDefault(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId);

            if (existingHod != null)
            {
                TempData["Error"] = "Bộ môn này đã có Trưởng bộ môn. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                return RedirectToAction("HeadOfDepartments");
            }

            var password = "123456";
            var success = await _authService.RegisterAsync(email, password, "HeadOfDepartment", firstName, lastName);
            if (success)
            {
                var user = _context.AppUsers.FirstOrDefault(u => u.Email == email);
                if (user != null)
                {
                    user.DepartmentId = departmentId;
                    _context.AppUsers.Update(user);
                    
                    _context.HodTerms.Add(new HodTerm {
                        AppUserId = user.Id,
                        DepartmentId = departmentId,
                        StartAt = DateTime.UtcNow
                    });
                    
                    await _context.SaveChangesAsync();

                    var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    await _auditLogService.LogAsync(adminId, "Create HOD", user.Id.ToString(), $"Email: {email}, Dept: {departmentId}");
                }
                TempData["Success"] = "Tạo tài khoản Trưởng bộ môn thành công. Mật khẩu mặc định là 123456.";
            }
            else
            {
                TempData["Error"] = "Email đã tồn tại.";
            }
            return RedirectToAction("HeadOfDepartments");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHodDepartment(int id, int? departmentId)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null || user.Role != "HeadOfDepartment")
            {
                TempData["Error"] = "Không tìm thấy Trưởng bộ môn.";
                return RedirectToAction("HeadOfDepartments");
            }

            if (departmentId.HasValue && user.DepartmentId != departmentId)
            {
                var existingHod = _context.AppUsers.FirstOrDefault(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId.Value && u.Id != id);

                if (existingHod != null)
                {
                    TempData["Error"] = "Bộ môn này đã có người quản lý. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                    return RedirectToAction("HeadOfDepartments");
                }
            }

            if (user.DepartmentId != departmentId)
            {
                var activeTerm = _context.HodTerms.FirstOrDefault(t => t.AppUserId == user.Id && t.EndAt == null);
                if (activeTerm != null) activeTerm.EndAt = DateTime.UtcNow;

                if (departmentId.HasValue)
                {
                    _context.HodTerms.Add(new HodTerm {
                        AppUserId = user.Id,
                        DepartmentId = departmentId.Value,
                        StartAt = DateTime.UtcNow
                    });
                }
            }

            user.DepartmentId = departmentId;
            _context.AppUsers.Update(user);
            await _context.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Update HOD", user.Id.ToString(), $"DeptId: {departmentId}");

            TempData["Success"] = "Đổi bộ môn cho HOD thành công.";
            return RedirectToAction("HeadOfDepartments");
        }



        [HttpGet]
        public async Task<IActionResult> Contacts()
        {
            var contactMessages = await _context.ContactMessages
                                        .Include(c => c.User)
                                        .OrderByDescending(c => c.CreatedAt)
                                        .ToListAsync();

            return View(contactMessages);
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // 1. Lấy 10 tài liệu học liệu mới nhất
            var documents = _context.Documents
                                    .Include(d => d.Subject)
                                    .OrderByDescending(d => d.UploadedAt)
                                    .Take(10)
                                    .ToList();

            var uploaders = _userRepository.Query()
                .Where(u => u.Role == "HeadOfDepartment")
                .ToList();

            var departments = _context.Departments.ToList();

            ViewBag.ActiveCount = _context.Documents.Count(d => d.IsActive == true);
            ViewBag.ProcessingCount = _context.Documents.Count(d => d.IsActive == false);

            int premiumUsersCount = _userRepository.Query().Count(u => u.Subscription == AppUser.SubscriptionType.Premium);

            long packagePrice = 100000;
            long totalRevenue = premiumUsersCount * packagePrice;

            ViewBag.PremiumCount = premiumUsersCount;
            ViewBag.TotalRevenue = totalRevenue;

            ViewBag.PendingContactsCount = _context.ContactMessages.Count(c => c.Status == ContactStatus.Pending);

            ViewBag.Uploaders = uploaders;
            ViewBag.Departments = departments;

            return View(documents);
        }

        // Action hỗ trợ Admin nhanh chóng GỠ BỎ học liệu lỗi trực tiếp từ trang Dashboard
        [HttpPost]
        public async Task<IActionResult> DeleteDocumentFromDashboard(int id)
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
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HeadOfDepartment,Student")]
        public IActionResult ViewDocument(int id)
        {
            // 1. Tìm thông tin tài liệu trong DB
            var document = _context.Documents.FirstOrDefault(d => d.Id == id);
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

            // 2. Bóc tách lấy tên file trần sạch sẽ
            string fileNameOnDisk = rawPath;
            if (fileNameOnDisk.StartsWith("local://", StringComparison.OrdinalIgnoreCase))
            {
                fileNameOnDisk = fileNameOnDisk.Substring(8);
            }

            if (fileNameOnDisk.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) ||
                fileNameOnDisk.StartsWith("uploads\\", StringComparison.OrdinalIgnoreCase))
            {
                fileNameOnDisk = fileNameOnDisk.Substring(8); // Cắt bỏ "uploads/" nếu bị lặp
            }

            // 3. TẠO DANH SÁCH BAO VÂY TẤT CẢ CÁC NƠI FILE CÓ THỂ TRỐN
            string projectRoot = _env.ContentRootPath; // Thư mục gốc dự án
            string webRoot = _env.WebRootPath;         // Thư mục wwwroot dự án

            var possiblePaths = new System.Collections.Generic.List<string>
            {
                Path.Combine(projectRoot, "uploads", fileNameOnDisk),
                Path.Combine(projectRoot, fileNameOnDisk)
            };

            // Nếu dự án có sử dụng wwwroot, thêm tiếp 2 vị trí trong wwwroot vào danh sách tìm kiếm
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

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        private string GetWelcomeEmailHtml(string firstName, string lastName, string email, string password)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #10b981;"">
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">RAG Chatbot System</h2>
                        <p style=""color: #64748b; margin-top: 8px; font-size: 14px;"">Hệ thống Trợ lý ảo & Quản lý Tài liệu</p>
                    </div>
                    <div style=""padding: 25px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Tài khoản của bạn đã được quản trị viên tạo thành công trên hệ thống. Dưới đây là thông tin đăng nhập dành cho bạn:</p>
                        <div style=""background-color: #f8fafc; padding: 20px; border-radius: 10px; border: 1px solid #e2e8f0; margin: 25px 0;"">
                            <p style=""margin: 0 0 10px 0; font-size: 15px;""><strong>Email:</strong> <span style=""color: #0f172a;"">{email}</span></p>
                            <p style=""margin: 0; font-size: 15px;""><strong>Mật khẩu mặc định:</strong> <span style=""background-color: #e2e8f0; padding: 4px 8px; border-radius: 6px; font-family: monospace; color: #0f172a; font-weight: bold; letter-spacing: 1px;"">{password}</span></p>
                        </div>
                        <p style=""color: #dc2626; font-size: 14px; background-color: #fef2f2; padding: 12px; border-radius: 8px; border-left: 4px solid #dc2626;""><strong>Lưu ý quan trọng:</strong> Đây là mật khẩu mặc định. Để bảo đảm an toàn, vui lòng đăng nhập và tiến hành đổi mật khẩu ngay lập tức tại phần <strong>Hồ sơ cá nhân</strong>.</p>
                    </div>
                    <div style=""text-align: center; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                        <a href=""https://localhost:5186/Auth/Login"" style=""display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);"">Đăng nhập vào Hệ thống</a>
                    </div>
                </div>";
        }
    }
}