using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly RagChatbot.Business.Interfaces.IAppUserService _userService;

        public IndexModel(RagChatbot.Business.Interfaces.IAppUserService userService)
        {
            _userService = userService;
        }

        public RagChatbot.Business.DTOs.AppUserDto UserProfile { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userEmail)) return RedirectToPage("/Auth/Login");

            var user = await _userService.GetByEmailAsync(userEmail);
            if (user == null) return RedirectToPage("/Auth/Login");

            UserProfile = user;
            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin.";
                return RedirectToPage();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và xác nhận không khớp.";
                return RedirectToPage();
            }

            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToPage();
            }

            var success = await _userService.VerifyAndChangePasswordAsync(userEmail, oldPassword, newPassword);

            if (!success)
            {
                TempData["Error"] = "Đổi mật khẩu thất bại. Kiểm tra lại mật khẩu cũ.";
                return RedirectToPage();
            }

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToPage();
        }
    }
}
