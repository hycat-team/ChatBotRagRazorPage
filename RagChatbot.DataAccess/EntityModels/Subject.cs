using System;
using System.Collections.Generic;

namespace RagChatbot.DataAccess.EntityModels
{
    public class Subject
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; } // Creator (Admin/HOD)
        public int? DepartmentId { get; set; }

        // Navigation Properties
        public AppUser? User { get; set; }
        public Department? Department { get; set; }
        public ICollection<SubjectAssignment> Assignments { get; set; } = new List<SubjectAssignment>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    }
}
