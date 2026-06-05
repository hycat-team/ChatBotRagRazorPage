using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using System.Security.Claims;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập mới được gửi liên hệ
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // API Tiếp nhận yêu cầu hỗ trợ từ Học sinh gửi lên qua AJAX
        [HttpPost]
        public async Task<IActionResult> SendContact(string content, ContactType type, int? relatedId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung liên hệ/báo lỗi!" });
            }

            // Lấy UserId của học sinh đang đăng nhập (Copy chuẩn từ cách làm của ông ở HomeController)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng thử lại!" });
            }

            try
            {
                // Tạo phiếu hỗ trợ mới
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

                return Json(new { success = true, message = "Gửi yêu cầu thành công! Ban quản trị sẽ xử lý sớm nhất có thể." });
            }
            catch (Exception ex)
            {
                // Tránh để crash ứng dụng nếu lỗi DB
                return Json(new { success = false, message = "Có lỗi xảy ra hệ thống: " + ex.Message });
            }
        }
    }
}
