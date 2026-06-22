namespace RagChatbot.DataAccess.EntityModels
{
    public class ChatSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }

        // Navigation Properties
        public Subject? Subject { get; set; }
        public AppUser? User { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
