using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json;

namespace RagChatbot.Business.Services
{
#pragma warning disable SKEXP0001 // Suppress experimental warnings for Semantic Kernel interfaces
#pragma warning disable CS0618
    public class GoogleEmbeddingService : ITextEmbeddingGenerationService
    {
        private readonly string[] _modelNames;
        private readonly string[] _apiKeys;
        private readonly HttpClient _httpClient;
        private int _currentIndex = 0;

        // Google Gemini free tier: 1500 req/min, but batchEmbedContents lets us send 100 texts/request
        private const int BatchSize = 100;
        private const int MaxRetries = 30;
        private const int InitialDelayMs = 5000;

        public GoogleEmbeddingService(string[] modelNames, string[] apiKeys, HttpClient? httpClient = null)
        {
            _modelNames = modelNames;
            _apiKeys = apiKeys;
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
            int delayMs = InitialDelayMs;
            int modelKeyCount = Math.Max(_modelNames.Length, _apiKeys.Length);
            int rotationAttempts = 0;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                var currentModelName = _modelNames[_currentIndex % _modelNames.Length];
                var currentApiKey = _apiKeys[_currentIndex % _apiKeys.Length];

                // Use batchEmbedContents endpoint — one HTTP call for up to 100 texts
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{currentModelName}:batchEmbedContents?key={currentApiKey}";

                var requestBody = new
                {
                    requests = batch.Select(text => new
                    {
                        model = $"models/{currentModelName}",
                        content = new { parts = new[] { new { text = text } } },
                        outputDimensionality = 768
                    }).ToArray()
                };

                var json = JsonSerializer.Serialize(requestBody);

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
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();

                if (statusCode == 429 || statusCode >= 500)
                {
                    if (attempt == MaxRetries)
                        throw new HttpRequestException($"Google Embedding API returned {statusCode} after {MaxRetries} retries for batch [{batchStart}..{batchStart + batch.Count - 1}] of {totalCount}. Error: {errorContent}");

                    rotationAttempts++;
                    if (modelKeyCount > 1 && rotationAttempts < modelKeyCount)
                    {
                        Interlocked.Increment(ref _currentIndex);
                        Console.WriteLine($"[GoogleEmbeddingService] Batch [{batchStart}/{totalCount}] got HTTP {statusCode}. Rotating to next model/key without delay...");
                        continue;
                    }


                    Console.WriteLine($"[GoogleEmbeddingService] Batch [{batchStart}/{totalCount}] got HTTP {statusCode}. Retry {attempt}/{MaxRetries} in {delayMs / 1000}s...");
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs = Math.Min(delayMs * 2, 60_000); // cap at 60s

                    rotationAttempts = 0;
                    Interlocked.Increment(ref _currentIndex);
                }
                else
                {
                    throw new HttpRequestException($"Google Embedding API returned unexpected status {statusCode}. Error: {errorContent}");
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
