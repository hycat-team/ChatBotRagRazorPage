using RagChatbot.Business.Interfaces;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using ExcelDataReader;
using System.Text;

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
                else if (extension == ".txt")
                {
                    return ExtractFromTxt(filePath);
                }
                else if (extension == ".xlsx")
                {
                    return ExtractFromXlsx(filePath);
                }
                else if (extension == ".pptx")
                {
                    return ExtractFromPptx(filePath);
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
                var body = wordDocument.MainDocumentPart?.Document?.Body;
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

        private List<PageContent> ExtractFromTxt(string filePath)
        {
            var result = new List<PageContent>();
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.Add(new PageContent
                {
                    PageNumber = 1,
                    Text = text
                });
            }
            return result;
        }

        private List<PageContent> ExtractFromXlsx(string filePath)
        {
            var result = new List<PageContent>();

            // Register encoding provider for ExcelDataReader support on .NET Core
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    int sheetIndex = 1;
                    do
                    {
                        var sheetName = reader.Name;
                        var sb = new StringBuilder();
                        sb.AppendLine($"--- Sheet: {sheetName} ---");

                        var rows = new List<List<string>>();
                        while (reader.Read())
                        {
                            var row = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var val = reader.GetValue(i)?.ToString() ?? "";
                                row.Add(val);
                            }
                            if (row.Any(v => !string.IsNullOrWhiteSpace(v)))
                            {
                                rows.Add(row);
                            }
                        }

                        foreach (var row in rows)
                        {
                            sb.AppendLine(string.Join(" | ", row));
                        }

                        var sheetText = sb.ToString();
                        if (!string.IsNullOrWhiteSpace(sheetText))
                        {
                            result.Add(new PageContent
                            {
                                PageNumber = sheetIndex++,
                                Text = sheetText
                            });
                        }
                    } while (reader.NextResult());
                }
            }

            return result;
        }

        private List<PageContent> ExtractFromPptx(string filePath)
        {
            var result = new List<PageContent>();

            using (var presentationDocument = PresentationDocument.Open(filePath, false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                if (presentationPart != null && presentationPart.Presentation != null && presentationPart.Presentation.SlideIdList != null)
                {
                    var slideIds = presentationPart.Presentation.SlideIdList.ChildElements;
                    for (int i = 0; i < slideIds.Count; i++)
                    {
                        var slideId = (SlideId)slideIds[i];
                        var relationshipId = slideId.RelationshipId;
                        if (relationshipId != null && relationshipId.HasValue)
                        {
                            var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId.Value!);
                            if (slidePart != null && slidePart.Slide != null)
                            {
                                var texts = new List<string>();

                                foreach (var paragraph in slidePart.Slide.Descendants<A.Paragraph>())
                                {
                                    var text = string.Join("", paragraph.Descendants<A.Text>().Select(t => t.Text));
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        texts.Add(text);
                                    }
                                }

                                var slideText = string.Join("\n", texts);
                                if (!string.IsNullOrWhiteSpace(slideText))
                                {
                                    result.Add(new PageContent
                                    {
                                        PageNumber = i + 1,
                                        Text = slideText
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
