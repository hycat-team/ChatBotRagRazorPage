using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;

namespace RagChatbot.Business.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly IDocumentRepository _docRepo;
        private readonly IDocumentChunkRepository _chunkRepo;
        private readonly IDocumentExtractionService _extractionService;
        private readonly ITextChunkingService _chunkingService;
        private readonly IAiService _aiService;
        private readonly IGoogleDriveService _driveService;
        private readonly ILocalStorageService _localStorage;
        private readonly ILogger<DocumentProcessingService> _logger;

        public DocumentProcessingService(
            IDocumentRepository docRepo,
            IDocumentChunkRepository chunkRepo,
            IDocumentExtractionService extractionService,
            ITextChunkingService chunkingService,
            IAiService aiService,
            IGoogleDriveService driveService,
            ILocalStorageService localStorage,
            ILogger<DocumentProcessingService> logger)
        {
            _docRepo = docRepo;
            _chunkRepo = chunkRepo;
            _extractionService = extractionService;
            _chunkingService = chunkingService;
            _aiService = aiService;
            _driveService = driveService;
            _localStorage = localStorage;
            _logger = logger;
        }

        public async Task ProcessNextPendingDocumentAsync(Func<Task>? onStatusChanged, CancellationToken stoppingToken)
        {
            var timeoutThreshold = DateTime.UtcNow.AddHours(-1);
            var document = await _docRepo.Query()
                .Where(d => d.Status == "Pending" || (d.Status == "Processing" && d.UploadedAt < timeoutThreshold))
                .FirstOrDefaultAsync(stoppingToken);

            if (document == null) return;

            try
            {
                // Mark as processing
                document.Status = "Processing";
                _docRepo.Update(document);
                await _docRepo.SaveChangesAsync();

                _logger.LogInformation($"Processing document: {document.FileName}");

                // Notify UI that it started processing
                if (onStatusChanged != null)
                    await onStatusChanged();

                // Resolve the file from the appropriate storage backend
                var tempFile = Path.GetTempFileName() + Path.GetExtension(document.FileName);
                IEnumerable<PageContent> pages;
                try
                {
                    Stream fileContent;
                    if (_localStorage.IsLocalPath(document.FilePath))
                    {
                        _logger.LogInformation("Reading '{FileName}' from local storage.", document.FileName);
                        fileContent = await _localStorage.ReadFileAsync(document.FilePath);
                    }
                    else
                    {
                        _logger.LogInformation("Downloading '{FileName}' from Google Drive.", document.FileName);
                        fileContent = await _driveService.DownloadFileAsync(document.FilePath);
                    }

                    using (fileContent)
                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await fileContent.CopyToAsync(fileStream);
                    }

                    pages = await _extractionService.ExtractTextAsync(tempFile);
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
                        if (carryOver.Length < 500)
                        {
                            pageList[i].Text = currentText.Substring(0, lastBoundary + 1).TrimEnd();
                            pageList[i + 1].Text = carryOver.TrimStart() + " " + nextText.TrimStart();
                        }
                    }
                }

                // Step 1: Extract all text chunks from all pages concurrently
                var allChunksBag = new ConcurrentBag<(int PageNumber, int ChunkIndex, string Text)>();

                await Parallel.ForEachAsync(pageList, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = stoppingToken }, async (page, ct) =>
                {
                    var chunks = await _chunkingService.ChunkTextAsync(page.Text);
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        allChunksBag.Add((PageNumber: page.PageNumber, ChunkIndex: i, Text: chunks[i]));
                    }
                });

                var allChunks = allChunksBag.OrderBy(c => c.PageNumber).ThenBy(c => c.ChunkIndex).Select(c => (c.PageNumber, c.Text)).ToList();

                if (allChunks.Count == 0)
                {
                    _logger.LogWarning($"Document {document.FileName} produced 0 chunks.");
                    document.Status = "Failed_NoText";
                    _docRepo.Update(document);
                    await _docRepo.SaveChangesAsync();

                    if (onStatusChanged != null)
                        await onStatusChanged();
                    return;
                }

                _logger.LogInformation($"Extracted {allChunks.Count} chunks. Starting batch embedding...");

                // Step 2: Generate all embeddings in one batched call
                var chunkTexts = allChunks.Select(c => c.Text).ToList();
                var embeddings = await _aiService.GenerateEmbeddingsAsync(chunkTexts);

                // Step 3: Save all chunks with their embeddings
                var chunksToSave = new List<DocumentChunk>(allChunks.Count);
                for (int i = 0; i < allChunks.Count; i++)
                {
                    chunksToSave.Add(new DocumentChunk
                    {
                        DocumentId = document.Id,
                        Content = allChunks[i].Text,
                        PageNumber = allChunks[i].PageNumber,
                        Embedding = new Vector(embeddings[i].ToArray())
                    });
                }
                await _chunkRepo.AddRangeAsync(chunksToSave);

                document.Status = "Indexed";
                _docRepo.Update(document);
                await _docRepo.SaveChangesAsync();
                await _chunkRepo.SaveChangesAsync();
                _logger.LogInformation($"Successfully indexed document: {document.FileName} ({allChunks.Count} chunks)");

                if (onStatusChanged != null)
                    await onStatusChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process document: {document.FileName}");

                document.Status = "Failed";
                _docRepo.Update(document);
                await _docRepo.SaveChangesAsync();

                if (onStatusChanged != null)
                    await onStatusChanged();
            }
        }
    }
}
