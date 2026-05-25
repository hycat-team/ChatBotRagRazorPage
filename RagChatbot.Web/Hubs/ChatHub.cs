using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RagChatbot.Web.Data;
using RagChatbot.Web.Models.Entities;
using RagChatbot.Web.Services;

namespace RagChatbot.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IVectorSearchService _vectorSearchService;
        private readonly IAiService _aiService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            ApplicationDbContext dbContext,
            IVectorSearchService vectorSearchService,
            IAiService aiService,
            ILogger<ChatHub> logger)
        {
            _dbContext = dbContext;
            _vectorSearchService = vectorSearchService;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task LoadSubjectHistory(int subjectId)
        {
            try
            {
                var session = await _dbContext.ChatSessions
                    .Include(s => s.Messages)
                    .Where(s => s.SubjectId == subjectId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (session != null)
                {
                    var messages = session.Messages.OrderBy(m => m.Timestamp).Select(m => new
                    {
                        role = m.Role,
                        content = m.Content,
                        citations = m.Citations
                    }).ToList();

                    await Clients.Caller.SendAsync("SessionLoaded", session.Id.ToString(), messages);
                }
                else
                {
                    await Clients.Caller.SendAsync("SessionLoaded", "", new List<object>()); // Empty session
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading history");
                await Clients.Caller.SendAsync("ReceiveError", "Không thể tải lịch sử chat.");
            }
        }

        public async Task SendMessage(string sessionIdStr, int subjectId, string message)
        {
            try
            {
                if (!Guid.TryParse(sessionIdStr, out var sessionId))
                {
                    // Create new session if invalid or empty
                    var session = new ChatSession { SubjectId = subjectId, Title = message.Length > 50 ? message.Substring(0, 50) + "..." : message };
                    _dbContext.ChatSessions.Add(session);
                    await _dbContext.SaveChangesAsync();
                    sessionId = session.Id;
                    await Clients.Caller.SendAsync("SessionCreated", sessionId.ToString());
                }

                // 1. Save user message
                var userMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "user",
                    Content = message
                };
                _dbContext.ChatMessages.Add(userMessage);
                await _dbContext.SaveChangesAsync();

                // 2 & 3. Skip Vector DB Search for simple greetings
                bool isGreeting = message.Length < 15 && !message.Contains('?');
                List<DocumentChunk> similarChunks = new List<DocumentChunk>();

                if (!isGreeting)
                {
                    var questionEmbedding = await _aiService.GenerateEmbeddingAsync(message);
                    similarChunks = await _vectorSearchService.SearchSimilarChunksAsync(subjectId, questionEmbedding);
                }

                // Construct Context string and Citations
                var contextBuilder = new System.Text.StringBuilder();
                var citationsList = new List<object>();

                foreach (var chunk in similarChunks)
                {
                    contextBuilder.AppendLine($"[File: {chunk.Document?.FileName}, Page: {chunk.PageNumber}]");
                    contextBuilder.AppendLine(chunk.Content);
                    contextBuilder.AppendLine("---");

                    citationsList.Add(new
                    {
                        FileName = chunk.Document?.FileName,
                        Page = chunk.PageNumber,
                        ContentSnippet = chunk.Content.Length > 100 ? chunk.Content.Substring(0, 100) + "..." : chunk.Content
                    });
                }

                var contextString = contextBuilder.ToString();
                var citationsJson = JsonSerializer.Serialize(citationsList);

                // 4. Build System Prompt
                var systemPrompt = $@"Bạn là trợ lý học tập thông minh. Bạn có thể trò chuyện, chào hỏi thân thiện. Tuy nhiên, khi trả lời các câu hỏi kiến thức, hãy chỉ dựa vào ngữ cảnh được cung cấp dưới đây.
Nếu câu hỏi yêu cầu kiến thức mà không có trong ngữ cảnh, hãy trả lời: 'Tôi không tìm thấy thông tin này trong tài liệu môn học'.
Tuyệt đối không sử dụng kiến thức bên ngoài để tự bịa câu trả lời kiến thức.

[NGỮ CẢNH TÀI LIỆU]:
{contextString}
";

                // Lấy 10 tin nhắn gần nhất để tạo ngữ cảnh giao tiếp (lịch sử chat), loại trừ tin nhắn hiện tại vừa lưu
                var history = await _dbContext.ChatMessages
                    .Where(x => x.SessionId == sessionId && x.Id != userMessage.Id)
                    .OrderByDescending(x => x.Timestamp)
                    .Take(10)
                    .ToListAsync();
                
                history.Reverse(); // Đảo ngược lại để theo đúng thứ tự thời gian cũ -> mới

                // 5. Get Streaming Response
                var stream = _aiService.GetChatStreamingResponseAsync(systemPrompt, message, history);

                var fullResponse = new System.Text.StringBuilder();
                
                // Indicate generation started
                await Clients.Caller.SendAsync("ReceiveToken", "", false); // trigger UI to create a new message bubble

                await foreach (var token in stream)
                {
                    fullResponse.Append(token);
                    await Clients.Caller.SendAsync("ReceiveToken", token, false);
                }

                // 6. Save Assistant Response
                var assistantMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = fullResponse.ToString(),
                    Citations = citationsJson
                };
                _dbContext.ChatMessages.Add(assistantMessage);
                await _dbContext.SaveChangesAsync();

                // 7. Send completion with citations
                await Clients.Caller.SendAsync("ReceiveToken", citationsJson, true); // true indicates stream ended, pass citations
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                await Clients.Caller.SendAsync("ReceiveError", "An error occurred while processing your message.");
            }
        }
    }
}
