#pragma warning disable SKEXP0050
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using RagChatbot.Business.Interfaces;

namespace RagChatbot.Business.Services
{
    public class TextChunkingService : ITextChunkingService
    {
        private readonly ILogger<TextChunkingService> _logger;

        public TextChunkingService(ILogger<TextChunkingService> logger)
        {
            _logger = logger;
        }

        public Task<List<string>> ChunkTextAsync(string rawText, int maxChunkSize = 400, int overlap = 50)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return Task.FromResult(new List<string>());

            // Clean repeating layout noise and remove global structural artifacts
            string cleanedText = Regex.Replace(rawText, @"NGUYÊN LÝ KẾ TOÁN\s*\.\s*", " ");

            // Capture and collapse all formatting spaces or layout newlines around numeric periods
            while (Regex.IsMatch(cleanedText, @"(\d+)\s*\.\s*(\d+)"))
            {
                cleanedText = Regex.Replace(cleanedText, @"(\d+)\s*\.\s*(\d+)", "$1ALPHANUMERICDOTMASK$2");
            }

            // Isolate markdown bold tag boundaries from word tokens
            cleanedText = cleanedText.Replace("**", " ** ");

            // Segment sanitized layout text into atomic lines without breaking commas
            var lines = TextChunker.SplitPlainTextLines(cleanedText, maxTokensPerLine: 350);

            // Group standalone lines into paragraph data payloads with continuous overlap
            var rawParagraphs = TextChunker.SplitPlainTextParagraphs(lines, maxChunkSize, overlap);

            // Reassemble ordered collection and decode hidden mask values
            var finalizedChunks = new List<string>();

            foreach (var paragraph in rawParagraphs)
            {
                // Revert masking strings back to standard numerical dots
                string restoredText = paragraph.Replace("ALPHANUMERICDOTMASK", ".").Trim();

                // Clean up accidental trailing fragments if they end with headers
                if (Regex.IsMatch(restoredText, @"\*\*\s*[^*]+\s*\*\*\s*$"))
                {
                    restoredText = Regex.Replace(restoredText, @"\*\*\s*[^*]+\s*\*\*\s*$", "").Trim();
                }

                if (!string.IsNullOrEmpty(restoredText))
                {
                    finalizedChunks.Add(restoredText);
                }
            }

            return Task.FromResult(finalizedChunks);
        }
    }
}
