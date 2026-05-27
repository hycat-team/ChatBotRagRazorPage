using RagChatbot.Business.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace RagChatbot.Business.Services
{
    

    public class DocumentExtractionService : IDocumentExtractionService
    {
        public Task<List<PageContent>> ExtractTextAsync(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return Task.Run(() =>
            {
                if (extension == ".pdf")
                {
                    return ExtractFromPdf(filePath);
                }
                else if (extension == ".docx")
                {
                    return ExtractFromDocx(filePath);
                }
                
                throw new NotSupportedException($"File format {extension} is not supported.");
            });
        }

        private List<PageContent> ExtractFromPdf(string filePath)
        {
            var result = new List<PageContent>();
            
            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    var text = page.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Add(new PageContent
                        {
                            PageNumber = page.Number,
                            Text = text
                        });
                    }
                }
            }
            
            return result;
        }

        private List<PageContent> ExtractFromDocx(string filePath)
        {
            var result = new List<PageContent>();
            
            using (var wordDocument = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDocument.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    // For DOCX, we don't have clear page numbers easily accessible via OpenXml.
                    // We'll treat the whole document as "Page 1".
                    var text = body.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Add(new PageContent
                        {
                            PageNumber = 1,
                            Text = text
                        });
                    }
                }
            }

            return result;
        }
    }
}

