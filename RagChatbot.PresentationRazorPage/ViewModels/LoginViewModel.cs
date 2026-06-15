using System.ComponentModel.DataAnnotations;

namespace RagChatbot.PresentationRazorPage.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
