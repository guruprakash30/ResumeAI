using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResumeAI.RagwithGraph.Api.Repository.Declaration;
using System.Text;
using UglyToad.PdfPig;

namespace ResumeAI.RagwithGraph.Api.Repository.Implementation
{
    public class ResumeTextExtractor : IResumeTextExtractor
    {
        private static string ExtractPdfText(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new StringBuilder();

            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            return sb.ToString();
        }

        private static string ExtractDocxText(Stream stream)
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document.Body;

            if (body == null)
                return string.Empty;

            return string.Join(
                Environment.NewLine,
                body.Descendants<Text>().Select(t => t.Text)
            );
        }

        private static async Task<string> ExtractPlainTextAsync(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }
        public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => ExtractPdfText(fileStream),
                ".docx" => ExtractDocxText(fileStream),
                ".txt" => await ExtractPlainTextAsync(fileStream),
                _ => throw new NotSupportedException($"Unsupported resume format: {extension}")
            };
        }
    }
}
