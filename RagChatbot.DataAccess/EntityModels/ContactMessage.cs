using System.ComponentModel.DataAnnotations.Schema;

namespace RagChatbot.DataAccess.EntityModels
{
    // 1. Định nghĩa các loại loại yêu cầu hỗ trợ
    public enum ContactType
    {
        DocumentIssue,   // Lỗi liên quan đến tài liệu học liệu
        ChatIssue,       // Lỗi liên quan đến AI/RAG Chatbot
        GeneralFeedback  // Góp ý, phản hồi chung hệ thống
    }

    // 2. Định nghĩa trạng thái xử lý của Admin
    public enum ContactStatus
    {
        Pending,    // Chờ xử lý (Mặc định khi học sinh mới gửi)
        Processing, // Đang trong quá trình giải quyết
        Resolved    // Đã xử lý xong xuôi
    }

    // 3. Model chính lưu dưới Database
    public class ContactMessage
    {
        public int Id { get; set; }

        // Người gửi yêu cầu
        public int UserId { get; set; } // ⚠️ LƯU Ý: Nếu Id của AppUser trong dự án của ông là kiểu 'string' hoặc 'Guid' thì hãy đổi kiểu dữ liệu ở đây lại cho khớp nhé!

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } = default!;

        // Nội dung phản hồi/kêu cứu từ học sinh
        public string Content { get; set; } = string.Empty;

        // Phân loại và Trạng thái (Dùng Enum cho chuyên nghiệp)
        public ContactType Type { get; set; }
        public ContactStatus Status { get; set; } = ContactStatus.Pending;

        // Cột liên kết mở rộng (Id của tài liệu hoặc Id phiên chat bị lỗi - nếu có)
        public int? RelatedId { get; set; }

        // Thời gian gửi yêu cầu
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property (Nếu ông muốn thiết lập liên kết khóa ngoại trực tiếp trong EF Core)
        // public virtual AppUser User { get; set; } = default!; = null!;
    }
}
