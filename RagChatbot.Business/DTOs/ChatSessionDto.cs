using System;

namespace RagChatbot.Business.DTOs
{
    public class ChatSessionDto
    {
        public Guid Id { get; set; }
        public int SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    
    public class CreateChatSessionDto
    {
        public int SubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}
