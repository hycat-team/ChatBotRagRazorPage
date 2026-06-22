namespace RagChatbot.DataAccess.EntityModels
{
    public class AppUser
    {
        public enum SubscriptionType
        {
            Free = 0,
            Premium = 1
        }
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

        public SubscriptionType Subscription { get; set; } = SubscriptionType.Free; // Mặc định là Free
        public int TodayChatCount { get; set; } = 0; // Đếm số câu hỏi trong ngày
        public DateTime LastActiveDate { get; set; } = DateTime.UtcNow.Date; // Lưu ngày hoạt động gần nhất để reset
    }
}
