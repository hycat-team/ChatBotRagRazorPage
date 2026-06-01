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

        private bool IsSimpleGreeting(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return true;
            var cleanMsg = msg.Trim().ToLower().Replace("?", "").Replace(".", "").Replace("!", "");
            
            var greetingKeywords = new HashSet<string>
            {
                "chào", "chào bạn", "hello", "hi", "hey", "alo", "chào bot", "chào ad", "xin chào", "hi ad", "hi bot"
            };
            
            return greetingKeywords.Contains(cleanMsg);
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

                // 2. Fetch recent conversation history early (excluding the current user message)
                var history = await _chatService.GetRecentSessionMessagesAsync(sessionId, 10, savedUserMsg.Id);

                // 3. Skip Vector DB Search for simple greetings, otherwise Rewrite Query
                string standaloneQuery = message;
                bool isGreeting = IsSimpleGreeting(message);
                List<RagChatbot.Business.DTOs.DocumentChunkDto> similarChunks = new List<RagChatbot.Business.DTOs.DocumentChunkDto>();

                if (!isGreeting)
                {
                    // Rewrite query using LLM based on chat history to get standalone query
                    standaloneQuery = await _aiService.RewriteQueryAsync(message, history);
                    _logger.LogInformation("Original Query: {OriginalQuery}, Standalone Query: {StandaloneQuery}", message, standaloneQuery);

                    var questionEmbedding = await _aiService.GenerateEmbeddingAsync(standaloneQuery);

                    if (documentIds != null)
                    {
                        documentIds = documentIds.Where(id => id > 0).ToList();
                    }

                    similarChunks = await _vectorSearchService.SearchSimilarChunksAsync(subjectId, questionEmbedding, topK: 15, documentIds: documentIds);
                }

                // ZERO_HALLUCINATION_POLICY: Return fallback if no relevant chunks are found
                if (!isGreeting && similarChunks.Count == 0)
                {
                    var fallbackMessage = "Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn.";
                    
                    await Clients.Caller.SendAsync("ReceiveToken", "", false); // Initialize bubble
                    await Clients.Caller.SendAsync("ReceiveToken", fallbackMessage, false);
                    
                    var assistantMsg = new RagChatbot.Business.DTOs.CreateChatMessageDto
                    {
                        SessionId = sessionId,
                        Role = "assistant",
                        Content = fallbackMessage,
                        Citations = "[]"
                    };
                    await _chatService.AddMessageAsync(assistantMsg);
                    await Clients.Caller.SendAsync("ReceiveToken", "[]", true);
                    return;
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

                // 4. Build System Prompt (STRICT_CONSTRAINTS)
                var systemPrompt = $@"Bạn là trợ lý học tập thông minh. Bạn có thể trò chuyện, chào hỏi thân thiện.
Tuy nhiên, đối với các câu hỏi tìm kiếm thông tin, bạn phải tuân thủ nghiêm ngặt GROUNDING_RULE: Chỉ sử dụng thông tin từ [NGỮ CẢNH TÀI LIỆU] dưới đây.
Tuyệt đối không sử dụng kiến thức bên ngoài. Nếu không có thông tin trong ngữ cảnh, hãy trả lời: 'Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn'.

[NGỮ CẢNH TÀI LIỆU]:
{contextString}
";

                // 5. Get Streaming Response
                // ISOLATION_RULE: Pass standaloneQuery as the input and NULL for history to avoid Data Leakage.
                var stream = _aiService.GetChatStreamingResponseAsync(systemPrompt, standaloneQuery, null);

                var fullResponse = new System.Text.StringBuilder();

                await Clients.Caller.SendAsync("ReceiveToken", "", false);

                await foreach (var token in stream)
                {
                    fullResponse.Append(token);
                    await Clients.Caller.SendAsync("ReceiveToken", token, false);
                }

                var finalResponseStr = fullResponse.ToString();

                if (finalResponseStr.Contains("Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn"))
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
