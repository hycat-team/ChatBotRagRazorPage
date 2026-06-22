using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RagChatbot.Business.Interfaces;
using System.Text.Json;
using System.Collections.Concurrent;

namespace RagChatbot.PresentationRazorPage.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly IVectorSearchService _vectorSearchService;
        private readonly IAiService _aiService;
        private readonly IDocumentService _documentService;
        private readonly IAppUserService _userService;
        private readonly ILogger<ChatHub> _logger;
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _activeGenerations = new();

        public ChatHub(
            IChatService chatService,
            ISubjectService subjectService,
            IVectorSearchService vectorSearchService,
            IAiService aiService,
            IDocumentService documentService,
            IAppUserService userService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _subjectService = subjectService;
            _vectorSearchService = vectorSearchService;
            _aiService = aiService;
            _documentService = documentService;
            _userService = userService;
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
                var user = await _userService.GetByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Tài khoản của bạn đã bị vô hiệu hóa. Phiên làm việc đã kết thúc.");
                    Context.Abort();
                    return;
                }

                var subject = await _subjectService.GetByIdAsync(subjectId);
                if (subject == null)
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Môn học không tồn tại.");
                    return;
                }

                var session = await _chatService.GetSessionBySubjectIdAsync(subjectId, userId);

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
                if (subject == null)
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Môn học không tồn tại.");
                    return;
                }

                var user = await _userService.GetByIdAsync(userId);
                if (user != null)
                {
                    if (!user.IsActive)
                    {
                        await Clients.Caller.SendAsync("ReceiveError", "Tài khoản của bạn đã bị vô hiệu hóa. Phiên làm việc đã kết thúc.");
                        Context.Abort();
                        return;
                    }

                    var today = DateTime.UtcNow.Date;

                    if (user.LastQueryDate.Date < today)
                    {
                        user.DailyQueryCount = 0;
                        user.LastQueryDate = DateTime.UtcNow;
                    }

                    if (user.LastActiveDate.Date < today)
                    {
                        user.TodayChatCount = 0;
                        user.LastActiveDate = today;
                    }

                    if (user.Role == "Student")
                    {
                        if (user.Subscription == "Free")
                        {
                            if (user.TodayChatCount >= 20)
                            {
                                await Clients.Caller.SendAsync("ReceiveError", "Bạn đã hết 20 lượt hỏi miễn phí của ngày hôm nay. Hãy nâng cấp gói Premium để chat không giới hạn nhé! 👑");
                                return;
                            }
                            user.TodayChatCount++;
                        }
                    }

                    bool isExemptFrom50Limit = user.Role == "Admin" || (user.Role == "Student" && user.Subscription == "Premium");
                    if (!isExemptFrom50Limit)
                    {
                        if (user.DailyQueryCount >= 50)
                        {
                            await Clients.Caller.SendAsync("ReceiveError", "Bạn đã vượt quá giới hạn 50 câu hỏi/ngày. Vui lòng quay lại vào ngày mai.");
                            return;
                        }
                    }

                    user.DailyQueryCount++;
                    await _userService.UpdateUserAsync(user);
                }

                if (!Guid.TryParse(sessionIdStr, out var sessionId))
                {
                    var title = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
                    var session = await _chatService.CreateSessionAsync(subjectId, userId, title);
                    sessionId = session.Id;
                    await Clients.Caller.SendAsync("SessionCreated", sessionId.ToString());
                }

                var realSessionIdStr = sessionId.ToString();
                using var cts = new CancellationTokenSource();
                _activeGenerations[realSessionIdStr] = cts;

                try
                {
                    var userMessage = new RagChatbot.Business.DTOs.CreateChatMessageDto
                    {
                        SessionId = sessionId,
                        Role = "user",
                        Content = message
                    };
                    var savedUserMsg = await _chatService.AddMessageAsync(userMessage);

                    var history = await _chatService.GetRecentSessionMessagesAsync(sessionId, 10, savedUserMsg.Id);

                    var allDocs = await _documentService.GetBySubjectIdAsync(subjectId);
                    bool hasActiveDocs = allDocs.Any(d => d.Status == "Indexed" && d.IsActive);

                    if (!hasActiveDocs)
                    {
                        var noDocFallback = "Hiện tại môn học chưa có tài liệu học tập được kích hoạt trên hệ thống. Vui lòng quay lại sau hoặc liên hệ Bộ môn phụ trách để biết thêm chi tiết.";

                        await Clients.Caller.SendAsync("ReceiveToken", "", false);
                        await Clients.Caller.SendAsync("ReceiveToken", noDocFallback, false);

                        var assistantMsgFallback = new RagChatbot.Business.DTOs.CreateChatMessageDto
                        {
                            SessionId = sessionId,
                            Role = "assistant",
                            Content = noDocFallback,
                            Citations = "[]"
                        };
                        await _chatService.AddMessageAsync(assistantMsgFallback);
                        await Clients.Caller.SendAsync("ReceiveToken", "[]", true);
                        return;
                    }

                    string standaloneQuery = message;
                    bool isGreeting = IsSimpleGreeting(message);
                    List<RagChatbot.Business.DTOs.DocumentChunkDto> similarChunks = new List<RagChatbot.Business.DTOs.DocumentChunkDto>();

                    if (!isGreeting)
                    {
                        standaloneQuery = await _aiService.RewriteQueryAsync(message, history);
                        _logger.LogInformation("Original Query: {OriginalQuery}, Standalone Query: {StandaloneQuery}", message, standaloneQuery);

                        var questionEmbedding = await _aiService.GenerateEmbeddingAsync(standaloneQuery);

                        if (documentIds != null)
                        {
                            documentIds = documentIds.Where(id => id > 0).ToList();
                            if (documentIds.Count == 0)
                            {
                                documentIds = null;
                            }
                        }

                        similarChunks = await _vectorSearchService.SearchSimilarChunksAsync(subjectId, standaloneQuery, questionEmbedding, topK: 15, documentIds: documentIds);
                    }
                    else
                    {
                        var greetingResponse = "Chào bạn! Mình là trợ lý thông minh. Mình có thể giúp gì cho bạn hôm nay?";

                        await Clients.Caller.SendAsync("ReceiveToken", "", false);
                        await Clients.Caller.SendAsync("ReceiveToken", greetingResponse, false);

                        var assistantMsgGreeting = new RagChatbot.Business.DTOs.CreateChatMessageDto
                        {
                            SessionId = sessionId,
                            Role = "assistant",
                            Content = greetingResponse,
                            Citations = "[]"
                        };
                        await _chatService.AddMessageAsync(assistantMsgGreeting);
                        await Clients.Caller.SendAsync("ReceiveToken", "[]", true);
                        return;
                    }

                    if (!isGreeting && similarChunks.Count == 0)
                    {
                        var fallbackMessage = "Hệ thống không tìm thấy thông tin về câu hỏi";

                        await Clients.Caller.SendAsync("ReceiveToken", "", false);
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
                        var dispName = string.IsNullOrWhiteSpace(chunk.Document?.DisplayName) ? chunk.Document?.FileName : chunk.Document?.DisplayName;
                        contextBuilder.AppendLine($"[{dispName}] - Trang {chunk.PageNumber}");
                        contextBuilder.AppendLine(chunk.Content);
                        contextBuilder.AppendLine("---");

                        var citationKey = $"{dispName}_{chunk.PageNumber}";
                        if (!seenCitations.Contains(citationKey))
                        {
                            seenCitations.Add(citationKey);
                            citationsList.Add(new
                            {
                                FileName = dispName,
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
Tuyệt đối không sử dụng kiến thức bên ngoài. 
LƯU Ý QUAN TRỌNG: Nếu câu hỏi của người dùng ngắn gọn, thiếu chủ ngữ (ví dụ: 'bao nhiêu tuổi?'), hãy chủ động suy luận từ các thông tin, nhân vật, hoặc sự kiện có trong ngữ cảnh để đưa ra câu trả lời hợp lý nhất.
Nếu hoàn toàn không có thông tin nào liên quan trong ngữ cảnh, hãy trả lời: 'Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn'.

[NGỮ CẢNH TÀI LIỆU]:
{contextString}
";

                    // 5. Get Streaming Response
                    var stream = _aiService.GetChatStreamingResponseAsync(systemPrompt, standaloneQuery, history, cts.Token);

                    var fullResponse = new System.Text.StringBuilder();
                    // bool wasCanceled = false;

                    await Clients.Caller.SendAsync("ReceiveToken", "", false);

                    try
                    {
                        await foreach (var token in stream.WithCancellation(cts.Token))
                        {
                            fullResponse.Append(token);
                            await Clients.Caller.SendAsync("ReceiveToken", token, false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // wasCanceled = true;
                        _logger.LogInformation("Generation stopped by user for session {SessionId}", realSessionIdStr);
                        fullResponse.Append("\n\n*(Đã dừng tạo)*");
                        await Clients.Caller.SendAsync("ReceiveToken", "\n\n*(Đã dừng tạo)*", false);
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
                finally
                {
                    _activeGenerations.TryRemove(realSessionIdStr, out _);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                await Clients.Caller.SendAsync("ReceiveError", "An error occurred while processing your message.");
            }
        }

        public Task StopGeneration(string sessionIdStr)
        {
            if (_activeGenerations.TryGetValue(sessionIdStr, out var cts))
            {
                cts.Cancel();
            }
            return Task.CompletedTask;
        }
    }
}
