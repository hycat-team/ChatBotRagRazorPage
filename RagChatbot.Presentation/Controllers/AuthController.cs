using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Interfaces; 
using RagChatbot.DataAccess.EntityModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.Presentation.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IAppUserRepository _userRepository; 

        public AuthController(IAuthService authService, IAppUserRepository userRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu người dùng ĐÃ ĐĂNG NHẬP TRƯỚC ĐÓ rồi mà cố tình vào lại trang Login
            if (User.Identity?.IsAuthenticated == true)
            {
                // 1. Nếu là Admin -> Đẩy thẳng về Dashboard Admin
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                // 2. Nếu là Giảng viên / Trưởng bộ môn -> Đẩy về kho tài liệu
                if (User.IsInRole("HeadOfDepartment"))
                {
                    return RedirectToAction("Index", "Document");
                }

                // 3. Nếu là Học sinh -> Đẩy về trang Chat
                return RedirectToAction("Index", "Home");
            }
            return View(new RagChatbot.Presentation.ViewModels.LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(RagChatbot.Presentation.ViewModels.LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _authService.AuthenticateAsync(model.Email, model.Password);
            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View(model);
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Nếu có đường dẫn ReturnUrl hợp lệ (ví dụ gõ trực tiếp url trước đó) thì đi theo ReturnUrl
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            // ─── ĐOẠN ĐIỀU HƯỚNG THÔNG MINH KHI ĐĂNG NHẬP THÀNH CÔNG ───

            // 1. Kiểm tra vai trò Admin trước
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin"); // Chuyển sang Admin/Index (Dashboard bạn vừa tạo)
            }

            // 2. Kiểm tra vai trò Giảng viên hoặc Trưởng bộ môn
            if (user.Role == "HeadOfDepartment")
            {
                return RedirectToAction("Index", "Document");
            }

            // 3. Mặc định cuối cùng dành cho Học sinh (Student) -> Vào trang Chat
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Redirect to login because public registration is disabled
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ─── TÍNH NĂNG NÂNG CẤP PREMIUM THỬ NGHIỆM (MOCK UPGRADE) ───
        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> UpgradeToPremium()
        {
            // 1. Lấy Id người dùng đang đăng nhập từ Cookies
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    // 2. Chuyển đổi gói thành Premium dựa theo đúng cấu trúc Enum lồng trong AppUser
                    user.Subscription = AppUser.SubscriptionType.Premium;

                    _userRepository.Update(user);
                    await _userRepository.SaveChangesAsync();

                    // 3. Đưa thông báo thành công ra màn hình để hiển thị Alert
                    TempData["Success"] = "Chúc mừng! Bạn đã nâng cấp tài khoản Premium thành công. Hãy tận hưởng kho dữ liệu và AI không giới hạn! ✨👑";

                    // 4. Quay trở về trang danh sách tài liệu
                    return RedirectToAction("Browse", "Document");
                }
            }
            return BadRequest("Không tìm thấy thông tin tài khoản hợp lệ.");
        }
    }
}