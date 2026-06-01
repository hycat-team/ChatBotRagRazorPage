using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RagChatbot.Business.Services;
using RagChatbot.Business.DTOs;

namespace TestRewrite
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Query Rewrite test...");

            // Load .env
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!File.Exists(envPath))
            {
                envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            }
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
                Console.WriteLine("Loaded .env file successfully from " + envPath);
            }
            else
            {
                Console.WriteLine(".env file not found.");
                return;
            }

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var chatModel = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gemma-4-26b-a4b-it";
            var fastChatModel = Environment.GetEnvironmentVariable("OPENAI_FAST_CHAT_MODEL") ?? "gemma-2-9b-it";

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"GoogleAi:ApiKey", apiKey ?? ""},
                    {"GoogleAi:ChatModel", chatModel},
                    {"GoogleAi:FastChatModel", fastChatModel}
                }!)
                .Build();

            var aiService = new AiService(config);

            // Test Chat Streaming
            Console.WriteLine("\n--- TESTING CHAT STREAMING ---");
            Console.WriteLine("Skipping streaming test because Gemma 26B API is currently hanging/timeout...");
            /*
            try
            {
                var responseStream = aiService.GetChatStreamingResponseAsync("Bạn là trợ lý vui vẻ.", "Xin chào, bạn tên gì?");
                await foreach (var token in responseStream)
                {
                    Console.Write(token);
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat streaming failed: {ex}");
            }
            */

            // Diagnostics: Try direct ChatHistory with history messages
            Console.WriteLine("\n--- DIAGNOSTIC 1: System Prompt + 1 User Message ---");
            try
            {
                var stream = aiService.GetChatStreamingResponseAsync("Bạn là trợ lý viết lại câu hỏi.", "Lịch sử: User hỏi tuổi Đức, Assistant trả lời 32. Hãy viết lại câu: 'Còn Duy thì sao'");
                await foreach (var token in stream) Console.Write(token);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Diagnostic 1 failed: {ex}");
            }

            Console.WriteLine("\n--- TEST CASE 1: Age Context ---");
            try
            {
                var historyCase1 = new List<ChatMessageDto>
                {
                    new ChatMessageDto { Role = "user", Content = "Duy bao nhiêu tuổi" },
                    new ChatMessageDto { Role = "assistant", Content = "Tuổi của Duy là 28." }
                };
                var queryCase1 = "Còn Huy thì sao";
                
                // 1. Rewrite Query
                var rewrittenCase1 = await aiService.RewriteQueryAsync(queryCase1, historyCase1);
                Console.WriteLine($"[Case 1] Original Query: {queryCase1}");
                Console.WriteLine($"[Case 1] Rewritten Query: {rewrittenCase1}");

                // 2. Chat generation
                var systemPromptCase1 = "Bạn là trợ lý học tập thông minh. Trả lời câu hỏi dựa trên ngữ cảnh sau:\nNgữ cảnh: Tuổi của Huy là 25.";
                var streamCase1 = aiService.GetChatStreamingResponseAsync(systemPromptCase1, queryCase1, historyCase1);
                Console.Write("[Case 1] AI Answer: ");
                await foreach (var token in streamCase1) Console.Write(token);
                Console.WriteLine("\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Case 1 failed: {ex}");
            }

            Console.WriteLine("\n--- TEST CASE 2: Hobby Context ---");
            try
            {
                var historyCase2 = new List<ChatMessageDto>
                {
                    new ChatMessageDto { Role = "user", Content = "Duy thích gì" },
                    new ChatMessageDto { Role = "assistant", Content = "Duy thích chơi game." }
                };
                var queryCase2 = "Còn Đức thì sao";
                
                // 1. Rewrite Query
                var rewrittenCase2 = await aiService.RewriteQueryAsync(queryCase2, historyCase2);
                Console.WriteLine($"[Case 2] Original Query: {queryCase2}");
                Console.WriteLine($"[Case 2] Rewritten Query: {rewrittenCase2}");

                // 2. Chat generation
                var systemPromptCase2 = "Bạn là trợ lý học tập thông minh. Trả lời câu hỏi dựa trên ngữ cảnh sau:\nNgữ cảnh: Đức thích đọc sách và chơi bóng đá.";
                var streamCase2 = aiService.GetChatStreamingResponseAsync(systemPromptCase2, queryCase2, historyCase2);
                Console.Write("[Case 2] AI Answer: ");
                await foreach (var token in streamCase2) Console.Write(token);
                Console.WriteLine("\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Case 2 failed: {ex}");
            }
        }
    }
}
