using System.Collections.Generic;

namespace RagChatbot.DataAccess.EntityModels
{
    public class AppUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";

        // Navigation Properties
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}
