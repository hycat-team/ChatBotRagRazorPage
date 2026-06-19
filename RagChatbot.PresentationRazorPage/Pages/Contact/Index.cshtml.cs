using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

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

        public async Task<IActionResult> OnPostSendContactAsync(string content, ContactType type, int? relatedId)
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
                    Type = type.ToString(),
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
