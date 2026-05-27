using RagChatbot.DataAccess.Interfaces;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.Data;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Repositories;
using RagChatbot.Business.Services;
using RagChatbot.Business.Interfaces;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RagChatbot.Presentation.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatSessionRepository _sessionRepo;
        private readonly IChatMessageRepository _messageRepo;
        private readonly ISubjectRepository _subjectRepo;
        private readonly IVectorSearchService _vectorSearchService;
        private readonly IAiService _aiService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatSessionRepository sessionRepo,
            IChatMessageRepository messageRepo,
            ISubjectRepository subjectRepo,
            IVectorSearchService vectorSearchService,
            IAiService aiService,
            ILogger<ChatHub> logger)
        {
            _sessionRepo = sessionRepo;
            _messageRepo = messageRepo;
            _subjectRepo = subjectRepo;
            _vectorSearchService = vectorSearchService;
            _aiService = aiService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            return int.TryParse(Context.UserIdentifier, out int userId) ? userId : 0;
        }

        public async Task LoadSubjectHistory(int subjectId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var subject = await _subjectRepo.GetByIdAsync(subjectId);
                if (subject == null || subject.UserId != userId)
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Unauthorized subject.");
                    return;
                }
                var session = await _sessionRepo.Query()
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

        public async Task SendMessage(string sessionIdStr, int subjectId, string message, List<int>? documentIds = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var subject = await _subjectRepo.GetByIdAsync(subjectId);
                if (subject == null || subject.UserId != userId)
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Unauthorized subject.");
                    return;
                }

                if (!Guid.TryParse(sessionIdStr, out var sessionId))
                {
                    // Create new session if invalid or empty
                    var session = new ChatSession { SubjectId = subjectId, Title = message.Length > 50 ? message.Substring(0, 50) + "..." : message };
                    await _sessionRepo.AddAsync(session);
                    await _sessionRepo.SaveChangesAsync();
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
                await _messageRepo.AddAsync(userMessage);
                await _messageRepo.SaveChangesAsync();

                // 2 & 3. Skip Vector DB Search for simple greetings
                bool isGreeting = message.Length < 15 && !message.Contains('?');
                List<DocumentChunk> similarChunks = new List<DocumentChunk>();

                if (!isGreeting)
                {
                    var questionEmbedding = await _aiService.GenerateEmbeddingAsync(message);
                    
                    // Filter out invalid IDs (e.g. 0 from NaN in frontend)
                    if (documentIds != null)
                    {
                        documentIds = documentIds.Where(id => id > 0).ToList();
                    }
                    
                    // Lấy 15 chunks thay vì 5 để tăng khả năng tìm thấy câu trả lời chính xác, 
                    // đặc biệt với các câu hỏi ngắn dễ bị nhầm lẫn vector do viết hoa/viết thường
                    similarChunks = await _vectorSearchService.SearchSimilarChunksAsync(subjectId, questionEmbedding, topK: 15, documentIds: documentIds);
                }

                // Construct Context string and Citations
                var contextBuilder = new System.Text.StringBuilder();
                var citationsList = new List<object>();
                var seenCitations = new HashSet<string>();

                foreach (var chunk in similarChunks)
                {
                    contextBuilder.AppendLine($"[File: {chunk.Document?.FileName}, Page: {chunk.PageNumber}]");
                    contextBuilder.AppendLine(chunk.Content);
                    contextBuilder.AppendLine("---");

                    var citationKey = $"{chunk.Document?.FileName}_{chunk.PageNumber}";
                    if (!seenCitations.Contains(citationKey))
                    {
                        seenCitations.Add(citationKey);
                        citationsList.Add(new
                        {
                            FileName = chunk.Document?.FileName,
                            Page = chunk.PageNumber,
                            ContentSnippet = chunk.Content.Length > 100 ? chunk.Content.Substring(0, 100) + "..." : chunk.Content
                        });
                    }
                }

                var contextString = contextBuilder.ToString();
                var citationsJson = JsonSerializer.Serialize(citationsList);

                // 4. Build System Prompt
                var systemPrompt = $@"Bạn là trợ lý học tập thông minh. Bạn có thể trò chuyện, chào hỏi thân thiện. Tuy nhiên, khi trả lời các câu hỏi kiến thức, hãy chỉ dựa vào ngữ cảnh được cung cấp dưới đây.
Nếu câu hỏi yêu cầu kiến thức mà không có trong ngữ cảnh, hãy trả lời: 'Tôi không tìm được câu trả lời trong tài liệu'.
Tuyệt đối không sử dụng kiến thức bên ngoài để tự bịa câu trả lời kiến thức.

[NGỮ CẢNH TÀI LIỆU]:
{contextString}
";

                // Lấy 10 tin nhắn gần nhất để tạo ngữ cảnh giao tiếp (lịch sử chat), loại trừ tin nhắn hiện tại vừa lưu
                var history = await _messageRepo.Query()
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

                var finalResponseStr = fullResponse.ToString();
                
                // Ẩn nguồn tham khảo nếu AI không tìm được câu trả lời để tránh hiển thị nguồn sai lệch
                if (finalResponseStr.Contains("Tôi không tìm được câu trả lời trong tài liệu"))
                {
                    citationsJson = "[]";
                }

                // 6. Save Assistant Response
                var assistantMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = finalResponseStr,
                    Citations = citationsJson
                };
                await _messageRepo.AddAsync(assistantMessage);
                await _messageRepo.SaveChangesAsync();

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
