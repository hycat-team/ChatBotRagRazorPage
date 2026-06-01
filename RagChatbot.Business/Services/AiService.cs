using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;
using System;
using System.Linq;
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

            Console.WriteLine($"[AiService Constructor] apiKey length: {firstApiKey?.Length ?? 0}, isGoogleKey: {isGoogleKey}, endpoint: '{endpoint}', chatModel: '{chatModel}', fastChatModel: '{fastChatModel}', embeddingModel: '{embeddingModelString}'");

            var builder = Kernel.CreateBuilder();
            var fastBuilder = Kernel.CreateBuilder();

            if (isGoogleKey && string.IsNullOrEmpty(endpoint))
            {
                if (string.IsNullOrEmpty(chatModel)) chatModel = "gemini-1.5-pro";
                if (string.IsNullOrEmpty(fastChatModel)) fastChatModel = "gemini-1.5-flash";
                
                var embeddingModels = string.IsNullOrEmpty(embeddingModelString) 
                    ? new[] { "text-embedding-004" } 
                    : embeddingModelString.Split(',').Select(m => m.Trim()).ToArray();

                Console.WriteLine($"[AiService Constructor] Configured native Google AI: chatModel={chatModel}, fastChatModel={fastChatModel}, embeddingModels={string.Join(",", embeddingModels)}");

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

                Console.WriteLine($"[AiService Constructor] Configured OpenAI/Custom: chatModel={chatModel}, fastChatModel={fastChatModel}, embeddingModel={singleEmbeddingModel}");

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
                    ? new[] { "text-embedding-004" } 
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

        public async IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto>? history = null)
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
                    { "max_tokens", 1000 },
                    { "temperature", 0.2 }
                }
            };

            var stream = _chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, _kernel);

            await foreach (var content in stream)
            {
                if (content.Content != null)
                {
                    yield return content.Content;
                }
            }
        }

        public async Task<string> RewriteQueryAsync(string originalQuery, IEnumerable<RagChatbot.Business.DTOs.ChatMessageDto> history)
        {
            if (history == null || !history.Any())
            {
                return originalQuery;
            }

            // Context Sliding Window: Take only the last 3 messages
            var recentHistory = history.TakeLast(3).ToList();
            var historyText = string.Join("\n", recentHistory.Select(m => $"{(m.Role == "user" ? "Người dùng" : "AI")}: {m.Content}"));

            var prompt = $@"You are a query rewriting assistant. Your task is to rewrite the [Current Query] into a STANDALONE query in Vietnamese based on the [Chat History].
RULES:
- The standalone query must include the full context and subject.
- Example: History asks about Duc's age, Current Query is ""Còn Duy thì sao"", you rewrite it as ""Duy bao nhiêu tuổi?"".
- DO NOT answer the question. ONLY output the rewritten query string.

[Chat History]
{historyText}

[Current Query]: ""{originalQuery}""

Rewritten Query:";

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);
            
            var executionSettings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    { "max_tokens", 100 },
                    { "temperature", 0.1 }
                }
            };

            try
            {
                var result = await _fastChatCompletion.GetChatMessageContentAsync(chatHistory, executionSettings, _kernel);
                var rewrittenQuery = result.Content?.Trim() ?? "";
                Console.WriteLine($"[RewriteQueryAsync Response]: '{rewrittenQuery}'");

                if (string.IsNullOrWhiteSpace(rewrittenQuery) || rewrittenQuery.Contains("Rewritten Query:"))
                {
                    return originalQuery;
                }

                return rewrittenQuery.Replace("\"", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RewriteQueryAsync Error]: {ex}");
                return originalQuery;
            }
        }
    }
}

