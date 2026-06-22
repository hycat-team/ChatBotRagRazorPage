namespace RagChatbot.Business.DTOs
{
    public class ContactMessageDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public AppUserDto User { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; }
        public string Status { get; set; } = default!;
        public int? RelatedId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
