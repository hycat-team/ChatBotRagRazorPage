using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;
using Pgvector;

namespace RagChatbot.Web.Services
{
    public interface IAiService
    {
        Task<Vector> GenerateEmbeddingAsync(string text);
        Task<List<Vector>> GenerateEmbeddingsAsync(IList<string> texts);
        IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, IEnumerable<RagChatbot.Web.Models.Entities.ChatMessage>? history = null);
    }

    public class AiService : IAiService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletion;
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly ITextEmbeddingGenerationService _embeddingGeneration;

        public AiService(IConfiguration configuration)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? configuration["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) apiKey = "dummy-key-to-prevent-crash";
            var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? configuration["OpenAI:Endpoint"];
            var chatModel = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? configuration["OpenAI:ChatModel"];
            var embeddingModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? configuration["OpenAI:EmbeddingModel"];

            Console.WriteLine($"[AiService Constructor] apiKey length: {apiKey?.Length ?? 0}, startsWithAIza: {apiKey?.StartsWith("AIza")}, endpoint: '{endpoint}', chatModel: '{chatModel}', embeddingModel: '{embeddingModel}'");

            var builder = Kernel.CreateBuilder();

            if (apiKey.StartsWith("AIza") && string.IsNullOrEmpty(endpoint))
            {
                if (string.IsNullOrEmpty(chatModel)) chatModel = "gemini-2.5-flash";
                if (string.IsNullOrEmpty(embeddingModel)) embeddingModel = "gemini-embedding-001";

                Console.WriteLine($"[AiService Constructor] Configured native Google AI: chatModel={chatModel}, embeddingModel={embeddingModel}");

                builder.AddGoogleAIGeminiChatCompletion(chatModel, apiKey);
                // Custom GoogleEmbeddingService will be instantiated below instead of using the builder
            }
            else
            {
                if (string.IsNullOrEmpty(chatModel)) chatModel = "gpt-4o-mini";
                if (string.IsNullOrEmpty(embeddingModel)) embeddingModel = "text-embedding-3-small";

                Console.WriteLine($"[AiService Constructor] Configured OpenAI/Custom: chatModel={chatModel}, embeddingModel={embeddingModel}");

                if (!string.IsNullOrEmpty(endpoint))
                {
                    var httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };
                    builder.AddOpenAIChatCompletion(chatModel, apiKey, httpClient: httpClient);
                    builder.AddOpenAITextEmbeddingGeneration(embeddingModel, apiKey, httpClient: httpClient);
                }
                else
                {
                    builder.AddOpenAIChatCompletion(chatModel, apiKey);
                    builder.AddOpenAITextEmbeddingGeneration(embeddingModel, apiKey);
                }
            }

            _kernel = builder.Build();
            _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            
            if (apiKey.StartsWith("AIza") && string.IsNullOrEmpty(endpoint))
            {
                _embeddingGeneration = new GoogleEmbeddingService(embeddingModel, apiKey);
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

        public async IAsyncEnumerable<string> GetChatStreamingResponseAsync(string systemPrompt, string userMessage, [EnumeratorCancellation] IEnumerable<RagChatbot.Web.Models.Entities.ChatMessage>? history = null)
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
