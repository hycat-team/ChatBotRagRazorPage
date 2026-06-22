using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IChatService
    {
        Task<ChatSessionDto?> GetSessionBySubjectIdAsync(int subjectId, int userId);
        Task<ChatSessionDto> CreateSessionAsync(int subjectId, int userId, string? title = null);
        Task<IEnumerable<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId);
        Task<IEnumerable<ChatMessageDto>> GetRecentSessionMessagesAsync(Guid sessionId, int limit, int? excludeMessageId = null);
        Task<ChatMessageDto> AddMessageAsync(CreateChatMessageDto message);
        Task ClearHistoryAsync(int subjectId, int userId);
        Task CleanupOldChatSessionsAsync(DateTime beforeDate);
    }
}
