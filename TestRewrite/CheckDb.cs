using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RagChatbot.DataAccess.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using Pgvector;

namespace TestRewrite
{
    class CheckDb
    {
        public static async Task Run()
        {
            DotNetEnv.Env.Load("../.env");
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? "Host=localhost;Port=5432;Database=RagChatbotDb;Username=postgres;Password=Password123!";
            
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());
            using var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();
            var config = configBuilder.Build();
            var aiService = new RagChatbot.Business.Services.AiService(config);

            var queryStr = "Duy bao nhiêu tuổi";
            var queryEmb = await aiService.GenerateEmbeddingAsync(queryStr);
            var queryVec = new Vector(queryEmb.ToArray());

            Console.WriteLine($"\n--- Distances for query: '{queryStr}' ---");
            var distances = await dbContext.DocumentChunks
                .Select(c => new { 
                    File = c.Document.FileName, 
                    Text = c.Content, 
                    Dist = c.Embedding.CosineDistance(queryVec) 
                })
                .ToListAsync();
                
            foreach (var c in distances.OrderBy(c => c.Dist)) {
                Console.WriteLine($"Doc: {c.File}, Text: {c.Text.Substring(0, Math.Min(c.Text.Length, 30))}..., Dist: {c.Dist}");
            }
        }
    }
}
