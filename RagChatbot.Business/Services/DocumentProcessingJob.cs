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

                // Pre-process pages to ensure no sentences are cut at page boundaries
                var pageList = pages.OrderBy(p => p.PageNumber).ToList();
                for (int i = 0; i < pageList.Count - 1; i++)
                {
                    var currentText = pageList[i].Text ?? "";
                    var nextText = pageList[i + 1].Text ?? "";
                    
                    if (string.IsNullOrWhiteSpace(currentText)) continue;
                    // Find the last true sentence boundary while skipping periods inside numbers
                    int lastBoundary = -1;
                    for (int j = currentText.Length - 1; j >= 0; j--)
                    {
                        char c = currentText[j];
                        if (c == '?' || c == '!' || c == '\n')
                        {
                            lastBoundary = j;
                            break;
                        }
                        if (c == '.')
                        {
                            // Skip this period if it resides between two digits (numeric separator)
                            bool prevIsDigit = j > 0 && char.IsDigit(currentText[j - 1]);
                            bool nextIsDigit = j < currentText.Length - 1 && char.IsDigit(currentText[j + 1]);
                            if (prevIsDigit && nextIsDigit)
                            {
                                continue;
                            }
                            lastBoundary = j;
                            break;
                        }
                    }
                    
                    if (lastBoundary >= 0 && lastBoundary < currentText.Length - 1)
                    {
                        string carryOver = currentText.Substring(lastBoundary + 1);
                        
                        // Only carry over if it's a partial sentence (less than 500 chars)
                        if (carryOver.Length < 500)
                        {
                            pageList[i].Text = currentText.Substring(0, lastBoundary + 1).TrimEnd();
                            pageList[i + 1].Text = carryOver.TrimStart() + " " + nextText.TrimStart();
                        }
                    }
                }

                // Step 1: Extract all text chunks from all pages concurrently
                var allChunksBag = new System.Collections.Concurrent.ConcurrentBag<(int PageNumber, int ChunkIndex, string Text)>();
                
                await Parallel.ForEachAsync(pageList, new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = stoppingToken }, async (page, ct) =>
                {
                    var chunks = await chunkingService.ChunkTextAsync(page.Text);
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        allChunksBag.Add((PageNumber: page.PageNumber, ChunkIndex: i, Text: chunks[i]));
                    }
                });

                var allChunks = allChunksBag.OrderBy(c => c.PageNumber).ThenBy(c => c.ChunkIndex).Select(c => (c.PageNumber, c.Text)).ToList();

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
