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
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class HeadOfDepartmentsModel : PageModel
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly RagChatbot.DataAccess.Data.ApplicationDbContext _context;

        public HeadOfDepartmentsModel(
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

        public System.Collections.Generic.List<AppUser> Hods { get; set; }

        public void OnGet()
        {
            var hods = _userRepository.Query()
                .Where(u => u.Role == "HeadOfDepartment" && u.IsActive)
                .ToList();

            var depts = _context.Departments.ToList();
            foreach (var hod in hods)
            {
                if (hod.DepartmentId.HasValue)
                {
                    hod.Department = depts.FirstOrDefault(d => d.Id == hod.DepartmentId.Value);
                }
            }

            Hods = hods;
            ViewData["Departments"] = depts;
        }

        public async Task<IActionResult> OnPostCreateHodAsync(string email, string firstName, string lastName, int departmentId)
        {
            var dept = await _context.Departments.FindAsync(departmentId);
            if (dept == null || !dept.IsActive)
            {
                TempData["Error"] = "Không thể gán Trưởng bộ môn cho bộ môn không tồn tại hoặc đang bị vô hiệu hóa.";
                return RedirectToPage();
            }

            var existingHod = _context.AppUsers.FirstOrDefault(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId);

            if (existingHod != null)
            {
                TempData["Error"] = "Bộ môn này đã có Trưởng bộ môn. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                return RedirectToPage();
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
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null || user.Role != "HeadOfDepartment")
            {
                TempData["Error"] = "Không tìm thấy Trưởng bộ môn.";
                return RedirectToPage();
            }

            if (departmentId.HasValue && user.DepartmentId != departmentId)
            {
                var dept = await _context.Departments.FindAsync(departmentId.Value);
                if (dept == null || !dept.IsActive)
                {
                    TempData["Error"] = "Không thể đổi sang bộ môn không tồn tại hoặc đang bị vô hiệu hóa.";
                    return RedirectToPage();
                }

                var existingHod = _context.AppUsers.FirstOrDefault(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId.Value && u.Id != id);

                if (existingHod != null)
                {
                    TempData["Error"] = "Bộ môn này đã có người quản lý. Mỗi bộ môn chỉ được phép có 1 Trưởng bộ môn.";
                    return RedirectToPage();
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

                user.DepartmentId = departmentId;
                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();
            }

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _auditLogService.LogAsync(adminId, "Update HOD", user.Id.ToString(), $"DeptId: {departmentId}");

            TempData["Success"] = "Đổi bộ môn cho HOD thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEndHodTermAsync(int id)
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
            return RedirectToPage();
        }


        public async Task<IActionResult> OnGetHodTermHistoryAsync(int userId)
        {
            var terms = await _context.HodTerms
                .Include(t => t.Department)
                .Where(t => t.AppUserId == userId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            var result = terms.Select(t => new {
                departmentName = t.Department != null ? t.Department.Name : "Không rõ",
                startAt = t.StartAt.ToString("dd/MM/yyyy"),
                endAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetDepartmentTermHistoryAsync(int deptId)
        {
            var terms = await _context.HodTerms
                .Include(t => t.AppUser)
                .Where(t => t.DepartmentId == deptId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            var result = terms.Select(t => new {
                hodName = t.AppUser != null ? (t.AppUser.LastName + " " + t.AppUser.FirstName) : "Không rõ",
                startAt = t.StartAt.ToString("dd/MM/yyyy"),
                endAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
            });

            return new JsonResult(result);
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






