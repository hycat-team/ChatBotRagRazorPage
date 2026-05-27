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
using Pgvector;

namespace RagChatbot.Business.Services
{
    

    public class AiService : IAiService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletion;
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ITextEmbeddingGenerationService _embeddingGeneration;

        public AiService(IConfiguration configuration)
        {
            var apiKeyString = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKeyString)) apiKeyString = "dummy-key-to-prevent-crash";
            var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? configuration["OpenAI:Endpoint"];
            var chatModel = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? configuration["OpenAI:ChatModel"];
            var embeddingModelString = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? configuration["OpenAI:EmbeddingModel"];

            var apiKeys = apiKeyString.Split(',').Select(k => k.Trim()).ToArray();
            var firstApiKey = apiKeys[0];

            Console.WriteLine($"[AiService Constructor] apiKey length: {firstApiKey?.Length ?? 0}, startsWithAIza: {firstApiKey?.StartsWith("AIza")}, endpoint: '{endpoint}', chatModel: '{chatModel}', embeddingModel: '{embeddingModelString}'");

            var builder = Kernel.CreateBuilder();

            if (firstApiKey != null && firstApiKey.StartsWith("AIza") && string.IsNullOrEmpty(endpoint))
            {
                if (string.IsNullOrEmpty(chatModel)) chatModel = "gemini-2.5-flash";
                
                var embeddingModels = string.IsNullOrEmpty(embeddingModelString) 
                    ? new[] { "gemini-embedding-001" } 
                    : embeddingModelString.Split(',').Select(m => m.Trim()).ToArray();

                Console.WriteLine($"[AiService Constructor] Configured native Google AI: chatModel={chatModel}, embeddingModels={string.Join(",", embeddingModels)}");

                builder.AddGoogleAIGeminiChatCompletion(chatModel, firstApiKey);
                // Custom GoogleEmbeddingService will be instantiated below instead of using the builder
            }
            else
            {
                if (string.IsNullOrEmpty(chatModel)) chatModel = "gpt-4o-mini";
                
                var singleEmbeddingModel = string.IsNullOrEmpty(embeddingModelString) 
                    ? "text-embedding-3-small" 
                    : embeddingModelString.Split(',').Select(m => m.Trim()).First();

                Console.WriteLine($"[AiService Constructor] Configured OpenAI/Custom: chatModel={chatModel}, embeddingModel={singleEmbeddingModel}");

                if (!string.IsNullOrEmpty(endpoint))
                {
                    var httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
                    builder.AddOpenAIChatCompletion(chatModel, firstApiKey, httpClient: httpClient);
                    builder.AddOpenAITextEmbeddingGeneration(singleEmbeddingModel, firstApiKey, httpClient: httpClient);
                }
                else
                {
                    builder.AddOpenAIChatCompletion(chatModel, firstApiKey);
                    builder.AddOpenAITextEmbeddingGeneration(singleEmbeddingModel, firstApiKey);
                }
            }

            _kernel = builder.Build();
            _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            
            if (firstApiKey != null && firstApiKey.StartsWith("AIza") && string.IsNullOrEmpty(endpoint))
            {
                var embeddingModels = string.IsNullOrEmpty(embeddingModelString) 
                    ? new[] { "gemini-embedding-001" } 
                    : embeddingModelString.Split(',').Select(m => m.Trim()).ToArray();
                    
                _embeddingGeneration = new GoogleEmbeddingService(embeddingModels, apiKeys);
            }
            else
            {
                _embeddingGeneration = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            }
        }

        public async Task<Vector> GenerateEmbeddingAsync(string text)
        {
            var result = await _embeddingGeneration.GenerateEmbeddingAsync(text);
            return new Vector(result.ToArray());
        }

        public async Task<List<Vector>> GenerateEmbeddingsAsync(IList<string> texts)
        {
            var results = await _embeddingGeneration.GenerateEmbeddingsAsync(texts);
            return results.Select(r => new Vector(r.ToArray())).ToList();
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        public async IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, [EnumeratorCancellation] IEnumerable<RagChatbot.DataAccess.EntityModels.ChatMessage>? history = null)
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

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 1000,
                Temperature = 0.2
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
    }
}

