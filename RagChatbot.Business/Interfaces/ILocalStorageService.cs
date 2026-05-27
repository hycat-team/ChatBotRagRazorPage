using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Pgvector;
using RagChatbot.DataAccess.EntityModels;

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
