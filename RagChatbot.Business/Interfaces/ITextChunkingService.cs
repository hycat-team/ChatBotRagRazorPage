using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface ITextChunkingService
    {
        Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000, int overlap = 200);
    }
}
