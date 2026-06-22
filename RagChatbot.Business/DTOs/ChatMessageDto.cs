namespace RagChatbot.Business.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Citations { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CreateChatMessageDto
    {
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Citations { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
