using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

using RagChatbot.DataAccess.EntityModels;

namespace RagChatbot.Business.Interfaces
{
    public class PageContent
    {
        public int PageNumber { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public interface IDocumentExtractionService
    {
        Task<List<PageContent>> ExtractTextAsync(string filePath);
    }
}
