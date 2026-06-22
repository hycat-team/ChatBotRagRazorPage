namespace RagChatbot.DataAccess.EntityModels
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int ActorId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string TargetObjectId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;

        // Optional navigation
        public AppUser? Actor { get; set; }
    }
}
