using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace RagChatbot.Business.Services
{
    public class TextChunkingService : ITextChunkingService
    {
        private readonly IAiService _aiService;
        private readonly ILogger<TextChunkingService> _logger;

        public TextChunkingService(IAiService aiService, ILogger<TextChunkingService> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000, int overlap = 200)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chunks;

            try
            {
                string systemPrompt = $@"SYSTEM PROMPT: SEMANTIC TEXT CHUNKING ENGINE

[ROLE]
You are a precise data engineering assistant specialized in text segmentation for RAG systems. Your task is to split the input Vietnamese text into a JSON array of clean, logically complete chunks.

[CRITICAL BOUNDARY CONSTRAINTS]
- ABSOLUTE WORD INTEGRITY: Never split a word across chunk boundaries. Every chunk must start and end with a complete, fully-formed word.
- SENTENCE BOUNDARY RULE: A chunk must only end at a valid sentence-ending punctuation mark, such as a period, question mark, or exclamation mark. Do not terminate a chunk mid-sentence.
- HEADER AND TITLE ISOLATION: When encountering markdown headers or bold titles, do not fuse them with the subsequent paragraph. Ensure there is a clean line break separating structural titles from body text.
- SMART SEMANTIC OVERLAP: For context continuity, each subsequent chunk must include an overlap consisting of the last one or two full sentences from the preceding chunk. This overlap must begin exactly at the start of a sentence, never mid-word.

[FORMAT SPECIFICATION]
Return only a raw, valid JSON array of strings. Do not include markdown formatting or wrapping like triple backticks. Do not append any sequential indicators or tracking numbers inside the text of the chunks.";

                string userMessage = $"[INPUT DATA]\nText_To_Process: \"\"\"{text}\"\"\"";

                var responseStream = _aiService.GetChatStreamingResponseAsync(systemPrompt, userMessage);
                var stringBuilder = new StringBuilder();

                await foreach (var content in responseStream)
                {
                    stringBuilder.Append(content);
                }

                string jsonResponse = stringBuilder.ToString().Trim();

                // Clean up possible markdown wrappers
                if (jsonResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    jsonResponse = jsonResponse.Substring(7);
                }
                else if (jsonResponse.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                {
                    jsonResponse = jsonResponse.Substring(3);
                }

                if (jsonResponse.EndsWith("```"))
                {
                    jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                }

                jsonResponse = jsonResponse.Trim();

                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var result = JsonSerializer.Deserialize<List<string>>(jsonResponse, options);
                if (result != null && result.Count > 0)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM Chunking failed or returned invalid JSON. Falling back to default algorithm.");
            }

            return FallbackChunkText(text, maxChunkSize, overlap);
        }

        private List<string> FallbackChunkText(string text, int maxChunkSize, int overlap)
        {
            var chunks = new List<string>();
            int i = 0;
            while (i < text.Length)
            {
                var length = Math.Min(maxChunkSize, text.Length - i);
                
                // End of chunk logic: try to end at a sentence boundary, or at least a space
                if (i + length < text.Length)
                {
                    int lastPunc = text.LastIndexOfAny(new[] { '.', '?', '!', '\n' }, i + length - 1, length);
                    if (lastPunc > i && lastPunc > i + (length / 2)) 
                    {
                        length = lastPunc - i + 1;
                    }
                    else
                    {
                        var lastSpace = text.LastIndexOf(' ', i + length - 1, length);
                        if (lastSpace > i)
                        {
                            length = lastSpace - i;
                        }
                    }
                }

                chunks.Add(text.Substring(i, length).Trim());
                
                if (i + length >= text.Length)
                {
                    break;
                }
                
                // Calculate next chunk start index with overlap
                var nextI = i + length - overlap;
                if (nextI <= i)
                {
                    break; // Prevent infinite loop
                }

                // Make sure nextI doesn't start mid-word. Try to start at a sentence boundary.
                int puncBeforeNextI = text.LastIndexOfAny(new[] { '.', '?', '!', '\n' }, nextI, nextI - i);
                if (puncBeforeNextI > i)
                {
                    nextI = puncBeforeNextI + 1;
                    // Skip any leading whitespace for the new chunk start
                    while (nextI < text.Length && char.IsWhiteSpace(text[nextI])) nextI++;
                }
                else
                {
                    var spaceBeforeNextI = text.LastIndexOf(' ', nextI, nextI - i);
                    if (spaceBeforeNextI > i) 
                    {
                        nextI = spaceBeforeNextI + 1;
                    }
                }

                i = nextI;
            }

            return chunks;
        }
    }
}
