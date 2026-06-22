using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using System.Runtime.CompilerServices;
#pragma warning disable CS0618

namespace RagChatbot.Business.Services
{


    public class AiService : IAiService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletion;
        private readonly IChatCompletionService _fastChatCompletion;
#pragma warning disable SKEXP0001
        private readonly ITextEmbeddingGenerationService _embeddingGeneration;

        public AiService(IConfiguration configuration)
        {
            var apiKeyString = (configuration["GoogleAi:ApiKey"] ?? configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY"))?.Trim();
            if (string.IsNullOrEmpty(apiKeyString)) apiKeyString = "dummy-key-to-prevent-crash";
            var endpoint = (configuration["GoogleAi:Endpoint"] ?? configuration["OpenAI:Endpoint"] ?? Environment.GetEnvironmentVariable("OPENAI_ENDPOINT"))?.Trim();

            var chatModel = (configuration["GoogleAi:ChatModel"] ?? configuration["OpenAI:ChatModel"] ?? Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL"))?.Trim();
            var fastChatModel = (configuration["GoogleAi:FastChatModel"] ?? configuration["OpenAI:FastChatModel"] ?? Environment.GetEnvironmentVariable("OPENAI_FAST_CHAT_MODEL"))?.Trim();
            var embeddingModelString = (configuration["GoogleAi:EmbeddingModel"] ?? configuration["OpenAI:EmbeddingModel"] ?? Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL"))?.Trim();

            var apiKeys = apiKeyString.Split(',')
                                      .Select(k => k.Trim())
                                      .Where(k => !string.IsNullOrEmpty(k))
                                      .ToArray();
            if (apiKeys.Length == 0) apiKeys = new[] { "dummy-key-to-prevent-crash" };

            var firstApiKey = apiKeys[0];
            var isGoogleKey = firstApiKey != null && (firstApiKey.StartsWith("AIza") || firstApiKey.StartsWith("AQ."));

            var builder = Kernel.CreateBuilder();
            var fastBuilder = Kernel.CreateBuilder();

            if (isGoogleKey && string.IsNullOrEmpty(endpoint))
            {
                if (string.IsNullOrEmpty(chatModel)) chatModel = "gemini-2.5-pro";
                if (string.IsNullOrEmpty(fastChatModel)) fastChatModel = "gemini-2.5-flash";

                var embeddingModels = string.IsNullOrEmpty(embeddingModelString)
                    ? new[] { "gemini-embedding-2-preview" }
                    : embeddingModelString.Split(',').Select(m => m.Trim()).ToArray();

                builder.AddGoogleAIGeminiChatCompletion(chatModel, firstApiKey!);
                fastBuilder.AddGoogleAIGeminiChatCompletion(fastChatModel, firstApiKey!);
            }
            else
            {
                if (string.IsNullOrEmpty(chatModel)) chatModel = "gpt-4o-mini";
                if (string.IsNullOrEmpty(fastChatModel)) fastChatModel = "gpt-4o-mini";

                var singleEmbeddingModel = string.IsNullOrEmpty(embeddingModelString)
                    ? "text-embedding-3-small"
                    : embeddingModelString.Split(',').Select(m => m.Trim()).First();

                if (!string.IsNullOrEmpty(endpoint))
                {
                    var httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
                    builder.AddOpenAIChatCompletion(chatModel, firstApiKey!, httpClient: httpClient);
                    builder.AddOpenAITextEmbeddingGeneration(singleEmbeddingModel, firstApiKey!, httpClient: httpClient);

                    fastBuilder.AddOpenAIChatCompletion(fastChatModel, firstApiKey!, httpClient: httpClient);
                }
                else
                {
                    builder.AddOpenAIChatCompletion(chatModel, firstApiKey!);
                    builder.AddOpenAITextEmbeddingGeneration(singleEmbeddingModel, firstApiKey!);

                    fastBuilder.AddOpenAIChatCompletion(fastChatModel, firstApiKey!);
                }
            }

            _kernel = builder.Build();
            _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

            var fastKernel = fastBuilder.Build();
            _fastChatCompletion = fastKernel.GetRequiredService<IChatCompletionService>();

            if (isGoogleKey && string.IsNullOrEmpty(endpoint))
            {
                var embeddingModels = string.IsNullOrEmpty(embeddingModelString)
                    ? new[] { "gemini-embedding-2-preview" }
                    : embeddingModelString.Split(',').Select(m => m.Trim()).ToArray();

                _embeddingGeneration = new GoogleEmbeddingService(embeddingModels, apiKeys);
            }
            else
            {
                _embeddingGeneration = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            }
        }

        public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
        {
            var result = await _embeddingGeneration.GenerateEmbeddingAsync(text);
            return result;
        }

        public async Task<List<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> texts)
        {
            var results = await _embeddingGeneration.GenerateEmbeddingsAsync(texts);
            return results.ToList();
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        public async IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto>? history = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var chatHistory = new ChatHistory(systemPrompt);

            if (history != null)
            {
                foreach (var msg in history)
                {
                    if (msg.Role == "user") chatHistory.AddUserMessage(msg.Content);
                    else if (msg.Role == "assistant") chatHistory.AddAssistantMessage(msg.Content);
                }
            }

            chatHistory.AddUserMessage(userMessage);

            var executionSettings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    { "max_tokens", 2048 },
                    { "temperature", 0.2 }
                }
            };

            int maxRetries = 3;
            int delayMs = 1500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                bool hasYielded = false;
                IAsyncEnumerator<StreamingChatMessageContent>? enumerator = null;
                bool retryNeeded = false;

                try
                {
                    var stream = _chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, _kernel, cancellationToken);
                    enumerator = stream.GetAsyncEnumerator(cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    if (attempt < maxRetries) { retryNeeded = true; } else throw new TimeoutException("Kết nối API bị quá hạn.");
                }
                catch (OperationCanceledException)
                {
                    throw; // Propagate to ChatHub
                }
                catch (Microsoft.SemanticKernel.HttpOperationException ex) when (attempt < maxRetries && (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError || ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (ex.Message != null && (ex.Message.Contains("500") || ex.Message.Contains("429")))))
                {
                    retryNeeded = true;
                }

                if (retryNeeded)
                {
                    Console.WriteLine($"[AiService] API Error/Timeout. Retrying ({attempt}/{maxRetries}) in {delayMs}ms...");
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2;
                    continue;
                }

                while (true)
                {
                    bool hasNext = false;
                    StreamingChatMessageContent? currentContent = null;

                    try
                    {
                        if (enumerator != null)
                        {
                            hasNext = await enumerator.MoveNextAsync();
                            if (hasNext) currentContent = enumerator.Current;
                        }
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        if (attempt < maxRetries && !hasYielded) { retryNeeded = true; } else throw new TimeoutException("Kết nối API bị quá hạn giữa chừng.");
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Propagate to ChatHub
                    }
                    catch (Microsoft.SemanticKernel.HttpOperationException ex) when (attempt < maxRetries && !hasYielded && (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError || ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests || (ex.Message != null && (ex.Message.Contains("500") || ex.Message.Contains("429")))))
                    {
                        retryNeeded = true;
                    }

                    if (retryNeeded)
                    {
                        Console.WriteLine($"[AiService] API Error during stream. Retrying ({attempt}/{maxRetries}) in {delayMs}ms...");
                        await Task.Delay(delayMs, cancellationToken);
                        delayMs *= 2;
                        if (enumerator != null) await enumerator.DisposeAsync();
                        break;
                    }

                    if (!hasNext)
                    {
                        if (enumerator != null) await enumerator.DisposeAsync();

                        // Retry if response is completely empty
                        if (!hasYielded && attempt < maxRetries)
                        {
                            Console.WriteLine($"[AiService] API returned empty response. Retrying ({attempt}/{maxRetries}) in {delayMs}ms...");
                            await Task.Delay(delayMs, cancellationToken);
                            delayMs *= 2;
                            break; // break while loop to continue for loop
                        }

                        yield break;
                    }

                    if (currentContent?.Content != null)
                    {
                        hasYielded = true;
                        yield return currentContent.Content;
                    }
                }
            }
        }

        public async Task<string> RewriteQueryAsync(string originalQuery, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto> history)
        {
            try
            {
                var historyList = history?.ToList() ?? new List<RagChatbot.Business.DTOs.ChatMessageDto>();

                // Build conversation history context
                var historyContext = "";
                if (historyList.Count > 0)
                {
                    var recentHistory = historyList.TakeLast(6);
                    var historyLines = recentHistory.Select(m => $"{(m.Role == "user" ? "Người dùng" : "Trợ lý")}: {m.Content}");
                    historyContext = string.Join("\n", historyLines);
                }

                var systemPrompt = @"Bạn là một trợ lý chuyên viết lại câu hỏi. Nhiệm vụ của bạn:
1. **Sửa lỗi chính tả và ngữ pháp** trong câu hỏi của người dùng (ví dụ: ""trhờ gian"" → ""thời gian"", ""giao trinh"" → ""giáo trình"", ""mấy tinh"" → ""máy tính"").
2. **Kết hợp ngữ cảnh từ lịch sử hội thoại** để tạo câu hỏi độc lập, rõ ràng (ví dụ: nếu trước đó đang nói về ""chương 3"" và người dùng hỏi ""nội dung chính là gì"" → ""Nội dung chính của chương 3 là gì?"").
3. **Giữ nguyên ý nghĩa gốc** - không thêm thông tin mới hay thay đổi ý định câu hỏi.
4. **Chỉ trả về câu hỏi đã viết lại**, không giải thích gì thêm.
5. Nếu câu hỏi đã rõ ràng và không có lỗi, trả về nguyên văn.";

                var chatHistory = new ChatHistory(systemPrompt);

                if (!string.IsNullOrEmpty(historyContext))
                {
                    chatHistory.AddUserMessage($"Lịch sử hội thoại:\n{historyContext}\n\nCâu hỏi hiện tại cần viết lại: {originalQuery}");
                }
                else
                {
                    chatHistory.AddUserMessage($"Câu hỏi cần viết lại (sửa chính tả nếu có): {originalQuery}");
                }

                var executionSettings = new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        { "max_tokens", 256 },
                        { "temperature", 0.0 }
                    }
                };

                var result = await _fastChatCompletion.GetChatMessageContentAsync(chatHistory, executionSettings, _kernel);
                var rewrittenQuery = result?.Content?.Trim();

                if (!string.IsNullOrWhiteSpace(rewrittenQuery))
                {
                    return rewrittenQuery;
                }

                return originalQuery;
            }
            catch (Exception)
            {
                // If rewriting fails, fall back to the original query silently
                return originalQuery;
            }
        }
    }
}

