using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Interfaces;
using System.Linq;
using System.Security.Claims;
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

        public AdminController(
            IAppUserRepository userRepository, 
            IAuthService authService, 
            IDocumentRepository documentRepository,
            IAuditLogService auditLogService,
            RagChatbot.DataAccess.Data.ApplicationDbContext context)
        {
            _userRepository = userRepository;
            _authService = authService;
            _documentRepository = documentRepository;
            _auditLogService = auditLogService;
            _context = context;
        }

        public async Task<IActionResult> Index(string searchEmail = "")
        {
            var query = _userRepository.Query().Where(u => u.Role != "Admin");
            
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
                    if (role == "Lecturer" && departmentId.HasValue)
                    {
                        var user = _userRepository.Query().FirstOrDefault(u => u.Email == email);
                        if (user != null)
                        {
                            user.DepartmentId = departmentId.Value;
                            _userRepository.Update(user);
                            await _userRepository.SaveChangesAsync();
                        }
                    }

                    if (emailService != null)
                    {
                        await emailService.SendEmailAsync(
                            email, 
                            "Tài khoản hệ thống RAG Chatbot", 
                            $"Xin chào {lastName} {firstName},\n\nTài khoản của bạn đã được tạo.\nEmail đăng nhập: {email}\nMật khẩu mặc định: {password}\n\nVui lòng đăng nhập và đổi mật khẩu."
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
        public async Task<IActionResult> UpdateLecturerDepartment(int userId, int? departmentId)
        {
            var user = _userRepository.Query().FirstOrDefault(u => u.Id == userId && u.Role == "Lecturer");
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy giảng viên.";
                return RedirectToAction("Index");
            }

            user.DepartmentId = departmentId;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Update Lecturer Dept", $"User {userId}", $"Dept: {departmentId}");

            TempData["Success"] = "Đã cập nhật bộ môn cho giảng viên.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null && user.Role != "Admin")
            {
                _userRepository.Remove(user);
                await _userRepository.SaveChangesAsync();
                
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
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null && user.Role != "Admin")
            {
                user.IsActive = !user.IsActive;
                _userRepository.Update(user);

                // Nếu khóa tài khoản, tắt (Inactive) toàn bộ tài liệu của Lecturer này (BR-06)
                if (!user.IsActive)
                {
                    var userDocs = _documentRepository.Query().Where(d => d.UploaderId == id).ToList();
                    foreach (var doc in userDocs)
                    {
                        doc.IsActive = false;
                        _documentRepository.Update(doc);
                    }
                    await _documentRepository.SaveChangesAsync();
                }

                await _userRepository.SaveChangesAsync();
                
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Toggle User Status", user.Id.ToString(), $"IsActive: {user.IsActive}");
                
                TempData["Success"] = $"{(user.IsActive ? "Mở khóa" : "Khóa")} tài khoản thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản hoặc không được phép thao tác.";
            }

            return RedirectToAction("Index");
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
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role ?? "Lecturer",
                DepartmentId = departmentId
            };

            await _userRepository.AddAsync(user);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Create Single User", user.Id.ToString(), $"Email: {email}, Role: {user.Role}");

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
            // Kiểm tra bộ môn đã có HOD chưa
            var existingHod = _userRepository.Query()
                .FirstOrDefault(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId);
                
            if (existingHod != null)
            {
                TempData["Error"] = "Bộ môn này đã có Trưởng bộ môn. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                return RedirectToAction("HeadOfDepartments");
            }

            var password = "123456";
            var success = await _authService.RegisterAsync(email, password, "HeadOfDepartment", firstName, lastName);
            if (success)
            {
                var user = _userRepository.Query().FirstOrDefault(u => u.Email == email);
                if (user != null)
                {
                    user.DepartmentId = departmentId;
                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();
                    
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
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.Role != "HeadOfDepartment")
            {
                TempData["Error"] = "Không tìm thấy Trưởng bộ môn.";
                return RedirectToAction("HeadOfDepartments");
            }

            if (departmentId.HasValue)
            {
                var existingHod = _userRepository.Query()
                    .FirstOrDefault(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId.Value && u.Id != id);
                    
                if (existingHod != null)
                {
                    TempData["Error"] = "Bộ môn này đã có người quản lý. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                    return RedirectToAction("HeadOfDepartments");
                }
            }

            user.DepartmentId = departmentId;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Update HOD", user.Id.ToString(), $"DeptId: {departmentId}");

            TempData["Success"] = "Đổi bộ môn cho HOD thành công.";
            return RedirectToAction("HeadOfDepartments");
        }
    }
}
