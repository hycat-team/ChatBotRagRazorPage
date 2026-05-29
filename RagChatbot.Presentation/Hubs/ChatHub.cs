using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using System.Text.Json;

namespace RagChatbot.Presentation.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly IVectorSearchService _vectorSearchService;
        private readonly IAiService _aiService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService,
            ISubjectService subjectService,
            IVectorSearchService vectorSearchService,
            IAiService aiService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _subjectService = subjectService;
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
                var subject = await _subjectService.GetByIdAsync(subjectId);
                if (subject == null || subject.UserId != userId)
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Unauthorized subject.");
                    return;
                }

                var session = await _chatService.GetSessionBySubjectIdAsync(subjectId);

                if (session != null)
                {
                    var messagesList = await _chatService.GetSessionMessagesAsync(session.Id);
                    var messages = messagesList.OrderBy(m => m.Timestamp).Select(m => new
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
                var subject = await _subjectService.GetByIdAsync(subjectId);
                if (subject == null || subject.UserId != userId)
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Unauthorized subject.");
                    return;
                }

                if (!Guid.TryParse(sessionIdStr, out var sessionId))
                {
                    var title = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
                    var session = await _chatService.CreateSessionAsync(subjectId, title);
                    sessionId = session.Id;
                    await Clients.Caller.SendAsync("SessionCreated", sessionId.ToString());
                }

                // 1. Save user message
                var userMessage = new RagChatbot.Business.DTOs.CreateChatMessageDto
                {
                    SessionId = sessionId,
                    Role = "user",
                    Content = message
                };
                var savedUserMsg = await _chatService.AddMessageAsync(userMessage);

                // 2 & 3. Skip Vector DB Search for simple greetings
                bool isGreeting = message.Length < 15 && !message.Contains('?');
                List<RagChatbot.Business.DTOs.DocumentChunkDto> similarChunks = new List<RagChatbot.Business.DTOs.DocumentChunkDto>();

                if (!isGreeting)
                {
                    var questionEmbedding = await _aiService.GenerateEmbeddingAsync(message);

                    if (documentIds != null)
                    {
                        documentIds = documentIds.Where(id => id > 0).ToList();
                    }

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

                // Lấy 10 tin nhắn gần nhất để tạo ngữ cảnh giao tiếp (lịch sử chat)
                var history = await _chatService.GetRecentSessionMessagesAsync(sessionId, 10, savedUserMsg.Id);

                // 5. Get Streaming Response
                var stream = _aiService.GetChatStreamingResponseAsync(systemPrompt, message, history);

                var fullResponse = new System.Text.StringBuilder();

                await Clients.Caller.SendAsync("ReceiveToken", "", false);

                await foreach (var token in stream)
                {
                    fullResponse.Append(token);
                    await Clients.Caller.SendAsync("ReceiveToken", token, false);
                }

                var finalResponseStr = fullResponse.ToString();

                if (finalResponseStr.Contains("Tôi không tìm được câu trả lời trong tài liệu"))
                {
                    citationsJson = "[]";
                }

                // 6. Save Assistant Response
                var assistantMessage = new RagChatbot.Business.DTOs.CreateChatMessageDto
                {
                    SessionId = sessionId,
                    Role = "assistant",
                    Content = finalResponseStr,
                    Citations = citationsJson
                };
                await _chatService.AddMessageAsync(assistantMessage);

                // 7. Send completion with citations
                await Clients.Caller.SendAsync("ReceiveToken", citationsJson, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                await Clients.Caller.SendAsync("ReceiveError", "An error occurred while processing your message.");
            }
        }
    }
}
