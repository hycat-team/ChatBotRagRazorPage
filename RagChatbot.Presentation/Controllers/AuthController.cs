using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.Business.Interfaces;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.Presentation.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Lecturer") || User.IsInRole("HeadOfDepartment"))
                {
                    return RedirectToAction("Index", "Document");
                }
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

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (user.Role == "Lecturer" || user.Role == "HeadOfDepartment")
            {
                return RedirectToAction("Index", "Document");
            }

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
    }
}
