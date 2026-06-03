using System.Collections.Generic;

namespace RagChatbot.DataAccess.EntityModels
{
    public class AppUser
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public bool IsActive { get; set; } = true;

        public int? DepartmentId { get; set; }
        public int DailyQueryCount { get; set; } = 0;
        public DateTime LastQueryDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Department? Department { get; set; }
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<SubjectAssignment> SubjectAssignments { get; set; } = new List<SubjectAssignment>();
    }
}
