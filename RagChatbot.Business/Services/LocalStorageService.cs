using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
namespace RagChatbot.Business.Services
{


    public class LocalStorageService : ILocalStorageService
    {
        private const string LocalPrefix = "local://";
        private readonly string _uploadDirectory;
        private readonly ILogger<LocalStorageService> _logger;

        public LocalStorageService(IWebHostEnvironment env, ILogger<LocalStorageService> logger)
        {
            _uploadDirectory = Path.Combine(env.WebRootPath, "uploads");
            _logger = logger;

            // Ensure directory exists
            Directory.CreateDirectory(_uploadDirectory);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
        {
            // Generate unique filename to avoid collisions
            var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
            var fullPath = Path.Combine(_uploadDirectory, uniqueName);

            using var fs = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(fs);

            _logger.LogInformation("File saved locally: {FileName}", uniqueName);

            // Return a prefixed path so DocumentProcessingJob knows it's local
            return $"{LocalPrefix}{uniqueName}";
        }

        public async Task<Stream> ReadFileAsync(string filePath)
        {
            var relativePath = GetRelativePath(filePath);
            var fullPath = Path.Combine(_uploadDirectory, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Local file not found: {relativePath}");

            var ms = new MemoryStream();
            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            await fs.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }

        public bool IsLocalPath(string filePath)
            => filePath?.StartsWith(LocalPrefix) == true;

        public string GetRelativePath(string filePath)
            => filePath?.StartsWith(LocalPrefix) == true
                ? filePath[LocalPrefix.Length..]
                : (filePath ?? string.Empty);
    }
}

