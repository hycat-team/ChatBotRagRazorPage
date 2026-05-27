using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Pgvector;
using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public interface ITextChunkingService
    {
        List<string> ChunkText(string text, int maxChunkSize = 1000, int overlap = 200);
    }
}
