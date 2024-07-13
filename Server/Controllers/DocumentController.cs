using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using NPOI.XWPF.UserModel;
using UglyToad.PdfPig.Content;

namespace template.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("File not selected or empty");

                var fileContentBuilder = new StringBuilder();
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (IsPdfFile(file, fileExtension))
                {
                    await ProcessPdfFileAsync(file, fileContentBuilder);
                }
                else if (IsDocxFile(file, fileExtension))
                {
                    await ProcessDocxFileAsync(file, fileContentBuilder);
                }
                else
                {
                    return BadRequest($"Unsupported file format. Supported formats are .pdf and .docx. Received file: {file.FileName}, Content-Type: {file.ContentType}");
                }

                return Ok(new { Content = fileContentBuilder.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while processing the file: {ex.Message}");
            }
        }

        private bool IsPdfFile(IFormFile file, string fileExtension)
        {
            return fileExtension == ".pdf" || file.ContentType == "application/pdf";
        }

        private bool IsDocxFile(IFormFile file, string fileExtension)
        {
            return fileExtension == ".docx" || file.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        }

        private async Task ProcessPdfFileAsync(IFormFile file, StringBuilder contentBuilder)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = UglyToad.PdfPig.PdfDocument.Open(memoryStream);
            foreach (var page in document.GetPages())
            {
                string pageText = ExtractTextFromPdfPage(page);
                contentBuilder.AppendLine(pageText);
            }
        }

        private async Task ProcessDocxFileAsync(IFormFile file, StringBuilder contentBuilder)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using var doc = new XWPFDocument(memoryStream);
            foreach (var paragraph in doc.Paragraphs)
            {
                contentBuilder.AppendLine(paragraph.ParagraphText);
            }
        }

        private string ExtractTextFromPdfPage(UglyToad.PdfPig.Content.Page page)
        {
            var words = page.GetWords().ToList();
            if (IsHebrewText(words))
            {
                words.Reverse();
                return string.Join(" ", words.Select(w => new string(w.Text.Reverse().ToArray())));
            }
            return string.Join(" ", words.Select(w => w.Text));
        }

        private bool IsHebrewText(IEnumerable<Word> words)
        {
            // Hebrew Unicode range
            var hebrewPattern = new Regex(@"[\u0590-\u05FF]");
            return words.Any(w => hebrewPattern.IsMatch(w.Text));
        }
    }
}