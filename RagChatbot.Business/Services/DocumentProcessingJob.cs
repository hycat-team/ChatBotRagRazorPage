using RagChatbot.DataAccess.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Repositories;
using Pgvector;

namespace RagChatbot.Business.Services
{
    public class DocumentProcessingJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DocumentProcessingJob> _logger;

        public DocumentProcessingJob(IServiceProvider serviceProvider, ILogger<DocumentProcessingJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Document Processing Job started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingDocumentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Document Processing Job.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ProcessPendingDocumentsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var chunkRepo = scope.ServiceProvider.GetRequiredService<IDocumentChunkRepository>();
            var extractionService = scope.ServiceProvider.GetRequiredService<IDocumentExtractionService>();
            var chunkingService = scope.ServiceProvider.GetRequiredService<ITextChunkingService>();
            var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
            var driveService = scope.ServiceProvider.GetRequiredService<IGoogleDriveService>();
            var localStorage = scope.ServiceProvider.GetRequiredService<ILocalStorageService>();

            var document = await docRepo.Query()
                .Where(d => d.Status == "Pending")
                .FirstOrDefaultAsync(stoppingToken);

            if (document == null) return;

            try
            {
                // Mark as processing
                document.Status = "Processing";
                docRepo.Update(document);
                await docRepo.SaveChangesAsync();

                _logger.LogInformation($"Processing document: {document.FileName}");

                // Resolve the file from the appropriate storage backend
                var tempFile = Path.GetTempFileName() + Path.GetExtension(document.FileName);
                IEnumerable<PageContent> pages;
                try
                {
                    Stream fileContent;
                    if (localStorage.IsLocalPath(document.FilePath))
                    {
                        _logger.LogInformation("Reading '{FileName}' from local storage.", document.FileName);
                        fileContent = await localStorage.ReadFileAsync(document.FilePath);
                    }
                    else
                    {
                        _logger.LogInformation("Downloading '{FileName}' from Google Drive.", document.FileName);
                        fileContent = await driveService.DownloadFileAsync(document.FilePath);
                    }

                    using (fileContent)
                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await fileContent.CopyToAsync(fileStream);
                    }

                    pages = await extractionService.ExtractTextAsync(tempFile);
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }

                // Step 1: Extract all text chunks from all pages
                var allChunks = new List<(int PageNumber, string Text)>();
                foreach (var page in pages)
                {
                    var chunks = await chunkingService.ChunkTextAsync(page.Text);
                    foreach (var chunkText in chunks)
                    {
                        allChunks.Add((PageNumber: page.PageNumber, Text: chunkText));
                    }
                }

                _logger.LogInformation($"Extracted {allChunks.Count} chunks. Starting batch embedding...");

                // Step 2: Generate all embeddings in one batched call (100 chunks per HTTP request)
                var chunkTexts = allChunks.Select(c => c.Text).ToList();
                var embeddings = await aiService.GenerateEmbeddingsAsync(chunkTexts);

                // Step 3: Save all chunks with their embeddings
                for (int i = 0; i < allChunks.Count; i++)
                {
                    var documentChunk = new DocumentChunk
                    {
                        DocumentId = document.Id,
                        Content = allChunks[i].Text,
                        PageNumber = allChunks[i].PageNumber,
                        Embedding = new Vector(embeddings[i].ToArray())
                    };
                    await chunkRepo.AddAsync(documentChunk);
                }

                document.Status = "Indexed";
                docRepo.Update(document);
                await docRepo.SaveChangesAsync();
                await chunkRepo.SaveChangesAsync();
                _logger.LogInformation($"Successfully indexed document: {document.FileName} ({allChunks.Count} chunks)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process document: {document.FileName}");

                document.Status = "Failed";
                docRepo.Update(document);
                await docRepo.SaveChangesAsync();
            }
        }
    }
}
