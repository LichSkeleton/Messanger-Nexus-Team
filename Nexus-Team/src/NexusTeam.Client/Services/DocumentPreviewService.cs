namespace NexusTeam.Client.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Wordprocessing;
    using UglyToad.PdfPig;

    /// <summary>
    /// Extracts text previews from PDF, Word, and plain-text attachments.
    /// </summary>
    public class DocumentPreviewService : IDocumentPreviewService
    {
        private const int MaxPdfPages = 20;
        private const int MaxPreviewCharacters = 50000;

        /// <inheritdoc/>
        public DocumentPreviewResult LoadPreview(string filePath, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => this.LoadPdf(filePath, fileName),
                ".docx" => this.LoadDocx(filePath, fileName),
                ".txt" or ".md" => this.LoadPlainText(filePath, fileName, extension == ".md" ? "Markdown" : "Text"),
                _ => throw new NotSupportedException($"Preview is not available for {extension} files."),
            };
        }

        private DocumentPreviewResult LoadPdf(string filePath, string fileName)
        {
            using var document = PdfDocument.Open(filePath);
            var builder = new StringBuilder();
            var pageCount = document.NumberOfPages;
            var pagesToRead = Math.Min(pageCount, MaxPdfPages);

            for (var pageNumber = 1; pageNumber <= pagesToRead; pageNumber++)
            {
                var page = document.GetPage(pageNumber);
                builder.AppendLine($"—— Page {pageNumber} of {pageCount} ——");
                builder.AppendLine();

                var pageText = page.Text;
                builder.AppendLine(string.IsNullOrWhiteSpace(pageText)
                    ? "[This page has no extractable text]"
                    : pageText.Trim());
                builder.AppendLine();

                if (builder.Length >= MaxPreviewCharacters)
                {
                    break;
                }
            }

            string? notice = null;
            if (pageCount > pagesToRead || builder.Length >= MaxPreviewCharacters)
            {
                notice = pageCount > pagesToRead
                    ? $"Showing the first {pagesToRead} of {pageCount} pages. Download the file to view the rest."
                    : "Preview was truncated. Download the file to view the full document.";
            }

            return new DocumentPreviewResult
            {
                FileName = fileName,
                FileTypeLabel = "PDF",
                TextContent = this.Truncate(builder.ToString()),
                Notice = notice,
            };
        }

        private DocumentPreviewResult LoadDocx(string filePath, string fileName)
        {
            using var word = WordprocessingDocument.Open(filePath, false);
            var body = word.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                return new DocumentPreviewResult
                {
                    FileName = fileName,
                    FileTypeLabel = "Word",
                    TextContent = "(Empty document)",
                };
            }

            var builder = new StringBuilder();
            foreach (var element in body.ChildElements)
            {
                if (element is Paragraph paragraph)
                {
                    builder.AppendLine(paragraph.InnerText);
                }
                else if (element is Table table)
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>().Select(cell => cell.InnerText);
                        builder.AppendLine(string.Join(" | ", cells));
                    }

                    builder.AppendLine();
                }

                if (builder.Length >= MaxPreviewCharacters)
                {
                    break;
                }
            }

            var text = builder.ToString().Trim();
            var wasTruncated = builder.Length >= MaxPreviewCharacters;
            return new DocumentPreviewResult
            {
                FileName = fileName,
                FileTypeLabel = "Word",
                TextContent = string.IsNullOrWhiteSpace(text) ? "(Empty document)" : this.Truncate(text),
                Notice = wasTruncated
                    ? "Preview was truncated. Download the file to view the full document."
                    : null,
            };
        }

        private DocumentPreviewResult LoadPlainText(string filePath, string fileName, string typeLabel)
        {
            var text = File.ReadAllText(filePath);
            var wasTruncated = text.Length > MaxPreviewCharacters;
            return new DocumentPreviewResult
            {
                FileName = fileName,
                FileTypeLabel = typeLabel,
                TextContent = string.IsNullOrWhiteSpace(text) ? "(Empty file)" : this.Truncate(text),
                Notice = wasTruncated
                    ? "Preview was truncated. Download the file to view the full file."
                    : null,
            };
        }

        private string Truncate(string text)
        {
            if (text.Length <= MaxPreviewCharacters)
            {
                return text;
            }

            return text.Substring(0, MaxPreviewCharacters).TrimEnd() + "...";
        }
    }
}
