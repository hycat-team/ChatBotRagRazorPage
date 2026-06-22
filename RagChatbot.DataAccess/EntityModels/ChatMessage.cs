namespace RagChatbot.DataAccess.EntityModels
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public Guid SessionId { get; set; }
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public string? Citations { get; set; } // JSON array string
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ChatSession? Session { get; set; }
    }
}
