using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TestRewrite
{
    class Program
    {
        static async Task Main(string[] args)
        {
            DotNetEnv.Env.Load("../.env");
            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
            var config = configBuilder.Build();
            var aiService = new RagChatbot.Business.Services.AiService(config);

            var history = new System.Collections.Generic.List<RagChatbot.Business.DTOs.ChatMessageDto>
            {
                new RagChatbot.Business.DTOs.ChatMessageDto { Role = "user", Content = "Hậu bao nhiêu tuổi" },
                new RagChatbot.Business.DTOs.ChatMessageDto { Role = "assistant", Content = "Hậu năm nay vừa tròn 30 tuổi." }
            };

            var query = "Huy thì bao nhiêu tuổi";
            var rewritten = await aiService.RewriteQueryAsync(query, history);
            Console.WriteLine($"Original: {query}");
            Console.WriteLine($"Rewritten: {rewritten}");
        }
    }
}
