namespace RagChatbot.Business.Interfaces
{
    public interface IGoogleDriveService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> DownloadFileAsync(string fileId);
    }
}
