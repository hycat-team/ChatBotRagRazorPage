using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace RagChatbot.Web.Services
{
#pragma warning disable SKEXP0001 // Suppress experimental warnings for Semantic Kernel interfaces
    public class GoogleEmbeddingService : ITextEmbeddingGenerationService
    {
        private readonly string _modelName;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        // Google Gemini free tier: 1500 req/min, but batchEmbedContents lets us send 100 texts/request
        private const int BatchSize = 100;
        private const int MaxRetries = 8;
        private const int InitialDelayMs = 3000;

        public GoogleEmbeddingService(string modelName, string apiKey, HttpClient? httpClient = null)
        {
            _modelName = modelName;
            _apiKey = apiKey;
            _httpClient = httpClient ?? new HttpClient();
        }

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
            IList<string> data,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            var allResults = new List<ReadOnlyMemory<float>>(data.Count);

            // Process in batches of up to 100 (Google's batchEmbedContents limit)
            for (int batchStart = 0; batchStart < data.Count; batchStart += BatchSize)
            {
                var batch = data.Skip(batchStart).Take(BatchSize).ToList();
                var batchResults = await EmbedBatchWithRetryAsync(batch, batchStart, data.Count, cancellationToken);
                allResults.AddRange(batchResults);
            }

            return allResults;
        }

        private async Task<List<ReadOnlyMemory<float>>> EmbedBatchWithRetryAsync(
            List<string> batch,
            int batchStart,
            int totalCount,
            CancellationToken cancellationToken)
        {
            // Use batchEmbedContents endpoint — one HTTP call for up to 100 texts
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:batchEmbedContents?key={_apiKey}";

            var requestBody = new
            {
                requests = batch.Select(text => new
                {
                    model = $"models/{_modelName}",
                    content = new { parts = new[] { new { text = text } } },
                    outputDimensionality = 768
                }).ToArray()
            };

            var json = JsonSerializer.Serialize(requestBody);

            int delayMs = InitialDelayMs;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                HttpResponseMessage response;
                using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                {
                    response = await _httpClient.PostAsync(url, content, cancellationToken);
                }

                if (response.IsSuccessStatusCode)
                {
                    using (response)
                    {
                        return ParseBatchResponse(await response.Content.ReadAsStringAsync(cancellationToken));
                    }
                }

                int statusCode = (int)response.StatusCode;
                response.Dispose();

                if (statusCode == 429 || statusCode >= 500)
                {
                    if (attempt == MaxRetries)
                        throw new HttpRequestException($"Google Embedding API returned {statusCode} after {MaxRetries} retries for batch [{batchStart}..{batchStart + batch.Count - 1}] of {totalCount}.");

                    Console.WriteLine($"[GoogleEmbeddingService] Batch [{batchStart}/{totalCount}] got HTTP {statusCode}. Retry {attempt}/{MaxRetries} in {delayMs / 1000}s...");
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs = Math.Min(delayMs * 2, 60_000); // cap at 60s
                }
                else
                {
                    throw new HttpRequestException($"Google Embedding API returned unexpected status {statusCode}.");
                }
            }

            throw new InvalidOperationException("Unreachable.");
        }

        private static List<ReadOnlyMemory<float>> ParseBatchResponse(string responseString)
        {
            var results = new List<ReadOnlyMemory<float>>();
            var doc = JsonDocument.Parse(responseString);

            // batchEmbedContents returns: { "embeddings": [ { "values": [...] }, ... ] }
            var embeddings = doc.RootElement.GetProperty("embeddings");
            foreach (var embedding in embeddings.EnumerateArray())
            {
                var valuesArray = embedding.GetProperty("values");
                var values = new float[valuesArray.GetArrayLength()];
                int i = 0;
                foreach (var val in valuesArray.EnumerateArray())
                    values[i++] = val.GetSingle();

                results.Add(new ReadOnlyMemory<float>(values));
            }

            return results;
        }
    }
#pragma warning restore SKEXP0001
}
