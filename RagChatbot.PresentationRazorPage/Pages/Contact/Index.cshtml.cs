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
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
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
                var newMessage = new ContactMessage
                {
                    UserId = userId,
                    Content = content.Trim(),
                    Type = type,
                    RelatedId = relatedId,
                    Status = ContactStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ContactMessages.Add(newMessage);
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Gửi yêu cầu thành công! Ban quản trị sẽ xử lý sớm nhất có thể." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra hệ thống: " + ex.Message });
            }
        }
    }
}
