namespace RagChatbot.Business.DTOs
{
    public class AppUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public bool IsActive { get; set; } = true;

        public int? DepartmentId { get; set; }
        public int DailyQueryCount { get; set; } = 0;
        public DateTime LastQueryDate { get; set; }
        public string Subscription { get; set; } = "Free";
        public int TodayChatCount { get; set; } = 0;
        public DateTime LastActiveDate { get; set; }

        public DepartmentDto? Department { get; set; }
    }
}
