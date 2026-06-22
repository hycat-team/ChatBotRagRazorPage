using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.Business.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatSessionRepository _sessionRepository;
        private readonly IChatMessageRepository _messageRepository;

        public ChatService(IChatSessionRepository sessionRepository, IChatMessageRepository messageRepository)
        {
            _sessionRepository = sessionRepository;
            _messageRepository = messageRepository;
        }

        public async Task<ChatSessionDto?> GetSessionBySubjectIdAsync(int subjectId, int userId)
        {
            var sessions = await _sessionRepository.FindAsync(s => s.SubjectId == subjectId && s.UserId == userId);
            return sessions.FirstOrDefault().ToDto();
        }

        public async Task<ChatSessionDto> CreateSessionAsync(int subjectId, int userId, string? title = null)
        {
            var dto = new CreateChatSessionDto
            {
                SubjectId = subjectId,
                Title = title ?? "New Session",
                UserId = userId
            };
            var session = dto.ToEntity();

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();
            return session.ToDto()!;
        }

        public async Task<IEnumerable<ChatMessageDto>> GetSessionMessagesAsync(Guid sessionId)
        {
            var messages = await _messageRepository.FindAsync(m => m.SessionId == sessionId);
            return messages.OrderBy(m => m.Timestamp).Select(m => m.ToDto()!).ToList();
        }

        public async Task<IEnumerable<ChatMessageDto>> GetRecentSessionMessagesAsync(Guid sessionId, int limit, int? excludeMessageId = null)
        {
            var query = _messageRepository.Query().Where(m => m.SessionId == sessionId);

            if (excludeMessageId.HasValue)
            {
                query = query.Where(m => m.Id != excludeMessageId.Value);
            }

            var messages = query.OrderByDescending(m => m.Timestamp).Take(limit).ToList();
            messages.Reverse(); // Return in chronological order
            return messages.Select(m => m.ToDto()!).ToList();
        }

        public async Task<ChatMessageDto> AddMessageAsync(CreateChatMessageDto dto)
        {
            var message = dto.ToEntity();
            if (message.Timestamp == default)
                message.Timestamp = DateTime.UtcNow;

            await _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync();
            return message.ToDto()!;
        }

        public async Task ClearHistoryAsync(int subjectId, int userId)
        {
            var session = await _sessionRepository.FindAsync(s => s.SubjectId == subjectId && s.UserId == userId);
            var firstSession = session.FirstOrDefault();
            if (firstSession != null)
            {
                var messages = await _messageRepository.FindAsync(m => m.SessionId == firstSession.Id);
                _messageRepository.RemoveRange(messages);
                await _messageRepository.SaveChangesAsync();
            }
        }

        public async Task CleanupOldChatSessionsAsync(DateTime beforeDate)
        {
            var oldSessions = _sessionRepository.Query().Where(s => s.CreatedAt < beforeDate).ToList();
            if (oldSessions.Any())
            {
                _sessionRepository.RemoveRange(oldSessions);
                await _sessionRepository.SaveChangesAsync();
            }
        }
    }
}
