using System;
using System.Collections.Generic;

namespace RagChatbot.DataAccess.EntityModels
{
    public class ChatSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Subject? Subject { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
