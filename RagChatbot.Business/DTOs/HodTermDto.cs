namespace RagChatbot.Business.DTOs
{
    public class HodTermDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string HodName { get; set; } = string.Empty;
        public string StartAt { get; set; } = string.Empty;
        public string? EndAt { get; set; }
    }
}
