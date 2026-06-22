namespace RagChatbot.DataAccess.EntityModels
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }

        // Navigation Properties
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    }
}
