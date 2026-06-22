using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;

namespace RagChatbot.Business.Services
{


    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly string _folderId;

        public GoogleDriveService(IConfiguration configuration)
        {
            var clientId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_CLIENT_ID") ?? configuration["GoogleDrive:ClientId"];
            var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_CLIENT_SECRET") ?? configuration["GoogleDrive:ClientSecret"];
            var refreshToken = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_REFRESH_TOKEN") ?? configuration["GoogleDrive:RefreshToken"];
            _folderId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_FOLDER_ID") ?? configuration["GoogleDrive:FolderId"] ?? "";

            var token = new TokenResponse { RefreshToken = refreshToken };
            var credentials = new UserCredential(new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    }
                }), "user", token);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentials,
                ApplicationName = "RagChatbot"
            });
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            if (string.IsNullOrEmpty(contentType) || contentType == "application/octet-stream")
            {
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                if (ext == ".pdf")
                {
                    contentType = "application/pdf";
                }
                else if (ext == ".docx")
                {
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                }
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                Parents = string.IsNullOrEmpty(_folderId) ? null : new List<string> { _folderId }
            };

            var request = _driveService.Files.Create(fileMetadata, fileStream, contentType);
            request.Fields = "id";

            var progress = await request.UploadAsync();
            if (progress.Status == UploadStatus.Failed)
            {
                throw new Exception($"Google Drive upload failed: {progress.Exception?.Message}");
            }

            var file = request.ResponseBody;
            return file.Id;
        }

        public async Task<Stream> DownloadFileAsync(string fileId)
        {
            var request = _driveService.Files.Get(fileId);
            var stream = new MemoryStream();
            var progress = await request.DownloadAsync(stream);
            if (progress.Status == Google.Apis.Download.DownloadStatus.Failed)
            {
                throw new Exception($"Google Drive download failed: {progress.Exception?.Message}");
            }
            stream.Position = 0;
            return stream;
        }
    }
}

