using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IAppUserService _userService;
        private readonly IDepartmentService _departmentService;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;

        public IndexModel(
            IAppUserService userService,
            IDepartmentService departmentService,
            IAuthService authService,
            IAuditLogService auditLogService)
        {
            _userService = userService;
            _departmentService = departmentService;
            _authService = authService;
            _auditLogService = auditLogService;
        }

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.AppUserDto> Users { get; set; } = default!;
        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.AppUserDto> BannedUsers { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string searchEmail = "")
        {
            Users = await _userService.GetUsersAsync("Student", true, searchEmail);
            BannedUsers = await _userService.GetUsersAsync("Student", false, searchEmail);
            ViewData["Departments"] = await _departmentService.GetAllDepartmentsAsync();
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
            var userDto = await _userService.GetByIdAsync(id);
            if (userDto != null && userDto.Role != "Admin")
            {
                await _userService.SoftDeleteUserAsync(id);

                var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Soft Delete User", id.ToString(), $"Email: {userDto.Email}");

                var emailQueue = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailQueue)) as RagChatbot.Business.Interfaces.IEmailQueue;
                if (emailQueue != null)
                {
                    var htmlBody = GetAccountLockedEmailHtml(userDto.FirstName, userDto.LastName, userDto.Email);
                    await emailQueue.QueueEmailAsync(new RagChatbot.Business.Interfaces.EmailMessage(
                        userDto.Email,
                        "Tài khoản của bạn đã bị vô hiệu hóa",
                        htmlBody
                    ));
                }

                TempData["Success"] = $"Đã xóa (ban) tài khoản {userDto.Email}.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRestoreUserAsync(int id)
        {
            var userDto = await _userService.GetByIdAsync(id);
            if (userDto != null)
            {
                await _userService.RestoreUserAsync(id);

                var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Restore User", id.ToString(), $"Email: {userDto.Email}");

                var emailQueue = HttpContext.RequestServices.GetService(typeof(RagChatbot.Business.Interfaces.IEmailQueue)) as RagChatbot.Business.Interfaces.IEmailQueue;
                if (emailQueue != null)
                {
                    var htmlBody = GetAccountRestoredEmailHtml(userDto.FirstName, userDto.LastName, userDto.Email);
                    await emailQueue.QueueEmailAsync(new RagChatbot.Business.Interfaces.EmailMessage(
                        userDto.Email,
                        "Tài khoản của bạn đã được khôi phục",
                        htmlBody
                    ));
                }

                TempData["Success"] = $"Đã khôi phục tài khoản {userDto.Email}.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(int id)
        {
            var userDto = await _userService.GetByIdAsync(id);
            if (userDto != null && userDto.Role != "Admin")
            {
                await _userService.ResetPasswordAsync(id, "123456");

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "Reset Password", id.ToString(), $"Email: {userDto.Email}");

                TempData["Success"] = $"Đặt lại mật khẩu thành công cho tài khoản {userDto.Email} (Mật khẩu mới: 123456).";
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

            var existingUser = await _userService.GetByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["Error"] = "Email đã tồn tại.";
                return RedirectToPage();
            }

            var userDto = new RagChatbot.Business.DTOs.AppUserDto
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role ?? "Student",
                DepartmentId = departmentId,
                IsActive = true
            };

            await _userService.AddUserAsync(userDto, password);

            var createdUser = await _userService.GetByEmailAsync(email);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Create Single User", createdUser?.Id.ToString() ?? "0", $"Email: {email}, Role: {userDto.Role}");

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

        private string GetWelcomeEmailHtml(string firstName, string lastName, string email, string password)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #10b981;"">
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">RAG Chatbot System</h2>
                        <p style=""color: #64748b; margin-top: 8px; font-size: 14px;"">Hệ thống Trợ lý ảo & Quản lý Tài liệu</p>
                    </div>
                    <div style=""padding: 30px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Tài khoản của bạn đã được quản trị viên tạo thành công trên hệ thống. Dưới đây là thông tin đăng nhập dành cho bạn:</p>
                        <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; padding: 20px; border-radius: 8px; margin: 25px 0;"">
                            <p style=""margin: 0 0 10px 0; font-size: 15px;""><strong>Email:</strong> <span style=""color: #0f172a;"">{email}</span></p>
                            <p style=""margin: 0; font-size: 15px;""><strong>Mật khẩu mặc định:</strong> <span style=""color: #0f172a; font-family: monospace; font-size: 16px; background-color: #e2e8f0; padding: 2px 6px; border-radius: 4px;"">{password}</span></p>
                        </div>
                        <p style=""color: #dc2626; font-size: 14px; background-color: #fef2f2; padding: 12px; border-radius: 8px; border-left: 4px solid #dc2626;""><strong>Lưu ý quan trọng:</strong> Đây là mật khẩu mặc định. Để bảo đảm an toàn, vui lòng đăng nhập và tiến hành đổi mật khẩu ngay lập tức tại phần <strong>Hồ sơ cá nhân</strong>.</p>
                    </div>
                    <div style=""text-align: center; padding-top: 25px; border-top: 1px solid #e2e8f0;"">
                        <a href=""https://localhost:5186/Auth/Login"" style=""display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);"">Đăng nhập vào Hệ thống</a>
                    </div>
                </div>";
        }

        private string GetAccountLockedEmailHtml(string firstName, string lastName, string email)
        {
            return $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 30px; border: 1px solid #fecdd3; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);"">
                    <div style=""text-align: center; padding-bottom: 25px; border-bottom: 2px solid #e11d48;"">
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">Thông báo Khóa tài khoản</h2>
                    </div>
                    <div style=""padding: 30px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chào <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Chúng tôi xin thông báo rằng tài khoản truy cập hệ thống của bạn (<strong>{email}</strong>) hiện đã bị <strong>Tạm khóa / Vô hiệu hóa</strong> bởi Quản trị viên.</p>
                        <div style=""background-color: #fff1f2; border: 1px solid #ffe4e6; padding: 15px; border-radius: 8px; margin: 25px 0; color: #be123c;"">
                            <p style=""margin: 0; font-size: 15px;"">Bạn sẽ không thể đăng nhập hoặc tiếp tục sử dụng các dịch vụ của hệ thống RAG Chatbot cho đến khi tài khoản được mở khóa.</p>
                        </div>
                        <p style=""font-size: 15px;"">Nếu bạn cho rằng đây là sự nhầm lẫn, vui lòng liên hệ với Quản trị viên hoặc phòng Đào tạo để được hỗ trợ kịp thời.</p>
                    </div>
                    <div style=""text-align: center; padding-top: 25px; border-top: 1px solid #e2e8f0; font-size: 13px; color: #64748b;"">
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
                            <span style=""color: #10b981; font-size: 32px; line-height: 64px;"">âœ“</span>
                        </div>
                        <h2 style=""color: #0f172a; margin: 0; font-size: 24px;"">TÃ i khoáº£n Ä‘Ã£ Ä‘Æ°á»£c KhÃ´i phá»¥c</h2>
                        <p style=""color: #64748b; margin-top: 8px; font-size: 14px;"">RAG Chatbot System</p>
                    </div>
                    <div style=""padding: 25px 0; color: #334155; line-height: 1.6;"">
                        <p style=""font-size: 16px; margin-bottom: 20px;"">Xin chÃ o <strong>{lastName} {firstName}</strong>,</p>
                        <p style=""font-size: 15px;"">Tuyá»‡t vá»i! TÃ i khoáº£n truy cáº­p há»‡ thá»‘ng cá»§a báº¡n (<strong>{email}</strong>) Ä‘Ã£ Ä‘Æ°á»£c <strong>KhÃ´i phá»¥c hoáº¡t Ä‘á»™ng</strong> bá»Ÿi Quáº£n trá»‹ viÃªn.</p>
                        <div style=""background-color: #f8fafc; padding: 20px; border-radius: 10px; border: 1px solid #e2e8f0; margin: 25px 0;"">
                            <p style=""margin: 0; font-size: 15px;"">Má»i dá»¯ liá»‡u vÃ  quyá»n lá»£i cá»§a báº¡n Ä‘Ã£ Ä‘Æ°á»£c khÃ´i phá»¥c nguyÃªn tráº¡ng. Báº¡n cÃ³ thá»ƒ tiáº¿p tá»¥c Ä‘Äƒng nháº­p vÃ  sá»­ dá»¥ng há»‡ thá»‘ng bÃ¬nh thÆ°á»ng ngay bÃ¢y giá».</p>
                        </div>
                    </div>
                    <div style=""text-align: center; padding-top: 30px; border-top: 1px solid #e2e8f0;"">
                        <a href=""https://localhost:5186/Auth/Login"" style=""display: inline-block; background-color: #10b981; color: #ffffff; text-decoration: none; padding: 12px 28px; border-radius: 8px; font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);"">ÄÄƒng nháº­p vÃ o Há»‡ thá»‘ng</a>
                    </div>
                </div>";
        }
    }
}



