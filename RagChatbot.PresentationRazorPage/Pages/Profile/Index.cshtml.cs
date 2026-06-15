using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.DataAccess.Interfaces;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IAppUserRepository _userRepository;

        public IndexModel(IAppUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public RagChatbot.DataAccess.EntityModels.AppUser UserProfile { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userEmail)) return RedirectToPage("/Auth/Login");

            var user = await _userRepository.GetByEmailAsync(userEmail);
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
            var user = await _userRepository.GetByEmailAsync(userEmail);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToPage();
            }

            var hashedOld = HashPassword(oldPassword);
            if (user.PasswordHash != hashedOld)
            {
                TempData["Error"] = "Mật khẩu cũ không chính xác.";
                return RedirectToPage();
            }

            user.PasswordHash = HashPassword(newPassword);
            await _userRepository.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToPage();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
