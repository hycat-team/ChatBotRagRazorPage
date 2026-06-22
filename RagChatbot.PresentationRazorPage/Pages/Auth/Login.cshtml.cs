using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.Business.Interfaces;
using RagChatbot.PresentationRazorPage.ViewModels;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;

        public LoginModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public LoginViewModel Input { get; set; } = new LoginViewModel();

        public IActionResult OnGet(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToPage("/Admin/Index");
                }
                if (User.IsInRole("HeadOfDepartment"))
                {
                    return RedirectToPage("/Document/Index");
                }
                return RedirectToPage("/Index");
            }
            Input.ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _authService.AuthenticateAsync(Input.Email, Input.Password);
            if (user == null)
            {
                ViewData["Error"] = "Email hoặc mật khẩu không chính xác.";
                return Page();
            }

            if (!user.IsActive)
            {
                ViewData["Error"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ.";
                return Page();
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

            if (!string.IsNullOrEmpty(Input.ReturnUrl) && Url.IsLocalUrl(Input.ReturnUrl))
            {
                return Redirect(Input.ReturnUrl);
            }

            if (user.Role == "Admin")
            {
                return RedirectToPage("/Admin/Index");
            }
            if (user.Role == "HeadOfDepartment")
            {
                return RedirectToPage("/Document/Index");
            }
            return RedirectToPage("/Index");
        }
    }
}
