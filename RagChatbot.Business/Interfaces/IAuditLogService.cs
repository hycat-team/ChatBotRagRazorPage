namespace RagChatbot.Business.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(int actorId, string action, string targetObjectId, string details);
    }
}
