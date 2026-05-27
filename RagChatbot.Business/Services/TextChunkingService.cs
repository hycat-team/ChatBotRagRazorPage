using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
namespace RagChatbot.Business.Services
{
    

    public class TextChunkingService : ITextChunkingService
    {
        public List<string> ChunkText(string text, int maxChunkSize = 1000, int overlap = 200)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chunks;

            int i = 0;
            while (i < text.Length)
            {
                var length = Math.Min(maxChunkSize, text.Length - i);
                
                // Don't cut in the middle of a word if possible
                if (i + length < text.Length)
                {
                    var lastSpace = text.LastIndexOf(' ', i + length - 1, length);
                    if (lastSpace > i)
                    {
                        length = lastSpace - i;
                    }
                }

                chunks.Add(text.Substring(i, length).Trim());
                
                if (i + length >= text.Length)
                {
                    break;
                }
                
                var nextI = i + length - overlap;
                if (nextI <= i)
                {
                    break;
                }
                i = nextI;
            }

            return chunks;
        }
    }
}

