using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Contact
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly RagChatbot.Business.Interfaces.IContactService _contactService;

        public IndexModel(RagChatbot.Business.Interfaces.IContactService contactService)
        {
            _contactService = contactService;
        }

        public async Task<IActionResult> OnPostSendContactAsync(string content, string type, int? relatedId)
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
