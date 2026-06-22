namespace RagChatbot.Business.Interfaces
{
    public interface ILocalStorageService
    {
        Task<string> SaveFileAsync(Stream fileStream, string fileName);
        Task<Stream> ReadFileAsync(string relativePath);
        bool IsLocalPath(string filePath);
        string GetRelativePath(string filePath);
    }
}
