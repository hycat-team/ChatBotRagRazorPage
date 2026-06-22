using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class HeadOfDepartmentsModel : PageModel
    {
        private readonly IAppUserService _userService;
        private readonly IDepartmentService _departmentService;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;

        public HeadOfDepartmentsModel(
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

        public System.Collections.Generic.IEnumerable<RagChatbot.Business.DTOs.AppUserDto> Hods { get; set; }

        public async Task OnGetAsync()
        {
            Hods = await _userService.GetUsersAsync("HeadOfDepartment", true, "");
            ViewData["Departments"] = await _departmentService.GetAllDepartmentsAsync();
        }

        public async Task<IActionResult> OnPostCreateHodAsync(string email, string firstName, string lastName, int departmentId)
        {
            var dept = await _departmentService.GetByIdAsync(departmentId);
            if (dept == null)
            {
                TempData["Error"] = "Không thể gán Trưởng bộ môn cho bộ môn không tồn tại.";
                return RedirectToPage();
            }

            var existsHod = await _userService.HasDepartmentHodAsync(departmentId);
            if (existsHod)
            {
                TempData["Error"] = "Bộ môn này đã có Trưởng bộ môn. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                return RedirectToPage();
            }

            var password = "123456";
            var success = await _authService.RegisterAsync(email, password, "HeadOfDepartment", firstName, lastName);
            if (success)
            {
                var user = await _userService.GetByEmailAsync(email);
                if (user != null)
                {
                    await _userService.UpdateHodDepartmentAsync(user.Id, departmentId);

                    var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    await _auditLogService.LogAsync(adminId, "Create HOD", user.Id.ToString(), $"Email: {email}, Dept: {departmentId}");

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
                }
                TempData["Success"] = "Tạo tài khoản Trưởng bộ môn thành công. Mật khẩu mặc định là 123456.";
            }
            else
            {
                TempData["Error"] = "Email đã tồn tại.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateHodDepartmentAsync(int id, int? departmentId)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null || user.Role != "HeadOfDepartment")
            {
                TempData["Error"] = "Không tìm thấy Trưởng bộ môn.";
                return RedirectToPage();
            }

            if (departmentId.HasValue && user.DepartmentId != departmentId)
            {
                var dept = await _departmentService.GetByIdAsync(departmentId.Value);
                if (dept == null)
                {
                    TempData["Error"] = "Không thể đổi sang bộ môn không tồn tại.";
                    return RedirectToPage();
                }

                var existsHod = await _userService.HasDepartmentHodAsync(departmentId.Value, id);
                if (existsHod)
                {
                    TempData["Error"] = "Bộ môn này đã có người quản lý. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                    return RedirectToPage();
                }
            }

            if (user.DepartmentId != departmentId)
            {
                await _userService.UpdateHodDepartmentAsync(id, departmentId);
            }

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Update HOD", id.ToString(), $"DeptId: {departmentId}");

            TempData["Success"] = "Đổi bộ môn cho HOD thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEndHodTermAsync(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null && user.Role == "HeadOfDepartment" && user.DepartmentId.HasValue)
            {
                await _userService.EndHodTermAsync(id);

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _auditLogService.LogAsync(adminId, "End HOD Term", id.ToString(), $"User: {user.Email}");

                TempData["Success"] = "Đã kết thúc nhiệm kỳ Trưởng bộ môn.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy Trưởng bộ môn hoặc không trong nhiệm kỳ.";
            }
            return RedirectToPage();
        }


        public async Task<IActionResult> OnGetHodTermHistoryAsync(int userId)
        {
            var result = await _userService.GetHodTermHistoryAsync(userId);
            return new JsonResult(result.Select(t => new
            {
                departmentName = t.DepartmentName,
                startAt = t.StartAt,
                endAt = t.EndAt
            }));
        }

        public async Task<IActionResult> OnGetDepartmentTermHistoryAsync(int deptId)
        {
            var result = await _userService.GetDepartmentTermHistoryAsync(deptId);
            return new JsonResult(result.Select(t => new
            {
                hodName = t.HodName,
                startAt = t.StartAt,
                endAt = t.EndAt
            }));
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






