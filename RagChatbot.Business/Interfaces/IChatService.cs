using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RagChatbot.Business.DTOs;

namespace RagChatbot.Business.Interfaces
{
    public interface IChatService
    {
        Task<ChatSessionDto?> GetSessionBySubjectIdAsync(int subjectId);
        Task<ChatSessionDto> CreateSessionAsync(int subjectId, string? title = null);
        Task<IEnumerable<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId);
        Task<IEnumerable<ChatMessageDto>> GetRecentSessionMessagesAsync(Guid sessionId, int limit, int? excludeMessageId = null);
        Task<ChatMessageDto> AddMessageAsync(CreateChatMessageDto message);
        Task ClearHistoryAsync(int subjectId);
    }
}
