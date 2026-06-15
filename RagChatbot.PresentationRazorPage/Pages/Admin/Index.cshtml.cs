using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;

        public IndexModel(
            IAppUserRepository userRepository,
            IAuthService authService,
            IAuditLogService auditLogService,
            RagChatbot.DataAccess.Data.ApplicationDbContext context)
        {
            _userRepository = userRepository;
            _authService = authService;
            _auditLogService = auditLogService;
            _context = context;
        }

        public System.Collections.Generic.List<AppUser> Users { get; set; }
        public System.Collections.Generic.List<AppUser> BannedUsers { get; set; }

        public async Task<IActionResult> OnGetAsync(string searchEmail = "")
        {
            var activeQuery = _userRepository.Query().Where(u => u.Role == "Student" && u.IsActive);
            var bannedQuery = _userRepository.Query().Where(u => u.Role == "Student" && !u.IsActive);
            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                activeQuery = activeQuery.Where(u => u.Email.Contains(searchEmail));
                bannedQuery = bannedQuery.Where(u => u.Email.Contains(searchEmail));
            }
            Users = activeQuery.ToList();
            BannedUsers = bannedQuery.ToList();
            ViewData["Departments"] = _context.Departments.ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateUsersAsync(Microsoft.AspNetCore.Http.IFormFile file, string role, int? departmentId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file chứa dữ liệu tài khoản.";
                return RedirectToPage();
            }

            using var reader = new System.IO.StreamReader(file.OpenReadStream());
            var userData = await reader.ReadToEndAsync();
            var lines = userData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            int successCount = 0;
            int failCount = 0;
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var emailQueue = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailQueue)) as RagChatbot.Business.Interfaces.IEmailQueue;

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
                    if (emailQueue != null)
                    {
                        var htmlBody = GetWelcomeEmailHtml(firstName, lastName, email, password);
                        await emailQueue.QueueEmailAsync(new RagChatbot.Business.Interfaces.EmailMessage(
                            email,
                            "Thông tin tài khoản RAG Chatbot",
                            htmlBody
                        ));
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
                TempData["Success"] = $"Tạo thành công {successCount} tài khoản. Bỏ qua {failCount} tài khoản do trùng lặp Email đã tồn tại hoặc sai định dạng.";
            }
            else
            {
                TempData["Success"] = $"Đã tạo thành công toàn bộ {successCount} tài khoản.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int id)
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

                user.IsActive = false;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Soft Delete User", user.Id.ToString(), $"Email: {user.Email}");

                var emailQueue = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailQueue)) as RagChatbot.Business.Interfaces.IEmailQueue;
                if (emailQueue != null)
                {
                    var htmlBody = GetAccountLockedEmailHtml(user.FirstName, user.LastName, user.Email);
                    await emailQueue.QueueEmailAsync(new RagChatbot.Business.Interfaces.EmailMessage(
                        user.Email,
                        "Tài khoản của bạn đã bị vô hiệu hóa",
                        htmlBody
                    ));
                }

                TempData["Success"] = $"Đã xóa (ban) tài khoản {user.Email}.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRestoreUserAsync(int id)
        {
            var user = await _context.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
            {
                user.IsActive = true;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();

                var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Restore User", user.Id.ToString(), $"Email: {user.Email}");

                var emailQueue = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailQueue)) as RagChatbot.Business.Interfaces.IEmailQueue;
                if (emailQueue != null)
                {
                    var htmlBody = GetAccountRestoredEmailHtml(user.FirstName, user.LastName, user.Email);
                    await emailQueue.QueueEmailAsync(new RagChatbot.Business.Interfaces.EmailMessage(
                        user.Email,
                        "Tài khoản của bạn đã được khôi phục",
                        htmlBody
                    ));
                }

                TempData["Success"] = $"Đã khôi phục tài khoản {user.Email}.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(int id)
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
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateSingleUserAsync(string email, string firstName, string lastName, string password, string role, int? departmentId)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập đủ thông tin.";
                return RedirectToPage();
            }

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["Error"] = "Email đã tồn tại.";
                return RedirectToPage();
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

            var emailQueue = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailQueue)) as RagChatbot.Business.Interfaces.IEmailQueue;
            if (emailQueue != null)
            {
                var htmlBody = GetWelcomeEmailHtml(firstName, lastName, email, password);
                await emailQueue.QueueEmailAsync(new RagChatbot.Business.Interfaces.EmailMessage(
                    email,
                    "Thông tin tài khoản RAG Chatbot",
                    htmlBody
                ));
            }

            TempData["Success"] = "Tạo người dùng thành công.";
            return RedirectToPage();
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

        private string GetAccountLockedEmailHtml(string firstName, string lastName, string email)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #ef4444;"">
                        <div style=""background-color: #fee2e2; width: 64px; height: 64px; border-radius: 50%; margin: 0 auto 15px auto; text-align: center;"">
                            <span style=""color: #ef4444; font-size: 32px; line-height: 64px;"">⚠️</span>
                        </div>
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">Thông báo Khóa tài khoản</h2>
                        <p style=""color: #64748b; margin-top: 8px; font-size: 14px;"">RAG Chatbot System</p>
                    </div>
                    <div style=""padding: 25px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Chúng tôi xin thông báo rằng tài khoản truy cập hệ thống của bạn (<strong>{email}</strong>) hiện đã bị <strong>Tạm khóa / Vô hiệu hóa</strong> bởi Quản trị viên.</p>
                        <div style=""background-color: #f8fafc; padding: 20px; border-radius: 10px; border: 1px solid #e2e8f0; margin: 25px 0;"">
                            <p style=""margin: 0; font-size: 15px;"">Bạn sẽ không thể đăng nhập hoặc tiếp tục sử dụng các dịch vụ của hệ thống RAG Chatbot cho đến khi tài khoản được mở khóa.</p>
                        </div>
                        <p style=""font-size: 15px;"">Nếu bạn cho rằng đây là sự nhầm lẫn, vui lòng liên hệ với Quản trị viên hoặc phòng Đào tạo để được hỗ trợ kịp thời.</p>
                    </div>
                    <div style=""text-align: center; padding-top: 30px; border-top: 1px solid #e2e8f0; color: #94a3b8; font-size: 13px;"">
                        Đây là email tự động, vui lòng không trả lời.
                    </div>
                </div>";
        }

        private string GetAccountRestoredEmailHtml(string firstName, string lastName, string email)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #10b981;"">
                        <div style=""background-color: #d1fae5; width: 64px; height: 64px; border-radius: 50%; margin: 0 auto 15px auto; text-align: center;"">
                            <span style=""color: #10b981; font-size: 32px; line-height: 64px;"">✓</span>
                        </div>
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">Tài khoản đã được Khôi phục</h2>
                        <p style=""color: #64748b; margin-top: 8px; font-size: 14px;"">RAG Chatbot System</p>
                    </div>
                    <div style=""padding: 25px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Tuyệt vời! Tài khoản truy cập hệ thống của bạn (<strong>{email}</strong>) đã được <strong>Khôi phục hoạt động</strong> bởi Quản trị viên.</p>
                        <div style=""background-color: #f8fafc; padding: 20px; border-radius: 10px; border: 1px solid #e2e8f0; margin: 25px 0;"">
                            <p style=""margin: 0; font-size: 15px;"">Mọi dữ liệu và quyền lợi của bạn đã được khôi phục nguyên trạng. Bạn có thể tiếp tục đăng nhập và sử dụng hệ thống bình thường ngay bây giờ.</p>
                        </div>
                    </div>
                    <div style=""text-align: center; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                        <a href=""https://localhost:5186/Auth/Login"" style=""display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);"">Đăng nhập vào Hệ thống</a>
                    </div>
                </div>";
        }
    }
}



