using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Contact
{
    [Authorize]
    [IgnoreAntiforgeryToken] //  THÊM DÒNG NÀY: Để không bị lỗi bảo mật 400 khi gọi AJAX từ trang khác sang
    public class SendContactModel : PageModel //  ĐÃ ĐỔI: Thành SendContactModel cho khớp đường dẫn
    {
        private readonly RagChatbot.Business.Interfaces.IContactService _contactService;

        public SendContactModel(RagChatbot.Business.Interfaces.IContactService contactService)
        {
            _contactService = contactService;
        }

        // ĐÃ ĐỔI: Thành OnPostAsync để hứng trọn đường dẫn /Contact/SendContact mà không cần truyền handler lên URL
        public async Task<IActionResult> OnPostAsync(string content, string type, int? relatedId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new JsonResult(new { success = false, message = "Vui lòng nhập nội dung liên hệ/báo lỗi!" });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng thử lại!" });
            }

            try
            {
                var newMessage = new RagChatbot.Business.DTOs.ContactMessageDto
                {
                    UserId = userId,
                    Content = content.Trim(),
                    Type = type,
                    RelatedId = relatedId
                };

                await _contactService.AddContactMessageAsync(newMessage);

                return new JsonResult(new { success = true, message = "Gửi yêu cầu thành công! Ban quản trị sẽ xử lý sớm nhất có thể." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra hệ thống: " + ex.Message });
            }
        }
    }
}
