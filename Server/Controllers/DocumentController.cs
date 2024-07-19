using System;
using System.Net.Http;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPOI.XWPF.UserModel;
using UglyToad.PdfPig.Content;
using System.Xml;
using HtmlAgilityPack;


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
            var words = page.GetWords().OrderBy(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left).ToList();
            var lines = new List<List<Word>>();
            var currentLine = new List<Word>();
            float? currentBottom = null;

            foreach (var word in words)
            {
                if (currentBottom == null || Math.Abs((float)word.BoundingBox.Bottom - currentBottom.Value) < 5)
                {
                    currentLine.Add(word);
                    currentBottom = (float)word.BoundingBox.Bottom;
                }
                else
                {
                    lines.Add(currentLine);
                    currentLine = new List<Word> { word };
                    currentBottom = (float)word.BoundingBox.Bottom;
                }
            }

            if (currentLine.Count > 0)
            {
                lines.Add(currentLine);
            }

            var extractedText = new StringBuilder();
            foreach (var line in lines)
            {
                var processedLine = ProcessLine(line);
                extractedText.AppendLine(processedLine);
            }

            return extractedText.ToString().Trim();
        }

        private string ProcessLine(List<Word> line)
        {
            var processedWords = line.Select(word => ProcessWord(word.Text)).ToList();
            return string.Join(" ", processedWords);
        }

        private string ProcessWord(string word)
        {
            if (IsHebrewText(word))
            {
                return new string(word.Reverse().ToArray());
            }
            return word;
        }

        private bool IsHebrewText(string text)
        {
            var hebrewPattern = new Regex(@"[\u0590-\u05FF]");
            return hebrewPattern.IsMatch(text);
        }

        private readonly IHttpClientFactory _clientFactory;


        //private readonly HttpClient _httpClient;

        //public DocumentController()
        //{
        //    _httpClient = new HttpClient();
        //}

        //[HttpGet("GetWikipediaContent")]
        //public async Task<IActionResult> GetWikipediaContent(string wikipediaLink)
        //{
        //    try
        //    {
        //        // Validate Wikipedia URL
        //        if (!ValidateWikipediaUrl(wikipediaLink))
        //        {
        //            return BadRequest("Invalid Wikipedia URL");
        //        }

        //        // Make HTTP request to fetch Wikipedia content
        //        HttpResponseMessage response = await _httpClient.GetAsync(wikipediaLink);
        //        response.EnsureSuccessStatusCode();

        //        // Read HTML content and clean it
        //        string htmlContent = await response.Content.ReadAsStringAsync();
        //        string cleanedText = CleanWikipediaHtml(htmlContent);

        //        return Ok(cleanedText);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error fetching Wikipedia content: {ex.Message}");
        //    }
        //}

        //private string CleanWikipediaHtml(string html)
        //{
        //    // Use HtmlAgilityPack to parse and clean HTML
        //    HtmlDocument doc = new HtmlDocument();
        //    doc.LoadHtml(html);

        //    // Remove unwanted elements
        //    RemoveElements(doc, "//script");
        //    RemoveElements(doc, "//style");

        //    // Get the text content from the cleaned document
        //    string cleanedText = doc.DocumentNode.InnerText;

        //    return cleanedText;
        //}

        //private void RemoveElements(HtmlDocument doc, string xpath)
        //{
        //    HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(xpath);
        //    if (nodes != null)
        //    {
        //        foreach (HtmlNode node in nodes)
        //        {
        //            node.Remove();
        //        }
        //    }
        //}

        //private bool ValidateWikipediaUrl(string wikipediaLink)
        //{
        //    if (string.IsNullOrEmpty(wikipediaLink))
        //    {
        //        return false;
        //    }

        //    // Example validation: Check if the link starts with https://en.wikipedia.org/wiki/ or https://de.wikipedia.org/wiki/
        //    // Modify this logic based on your requirements
        //    if (!Regex.IsMatch(wikipediaLink, @"^https:\/\/[a-z]{2}\.wikipedia\.org\/wiki\/", RegexOptions.IgnoreCase))
        //    {
        //        return false;
        //    }

        //    // Add more validation as needed

        //    return true;
        //}







        private readonly HttpClient _httpClient;

        public DocumentController()
        {
            _httpClient = new HttpClient();
        }

        [HttpGet("GetWikipediaContent")]
        public async Task<IActionResult> GetWikipediaContent(string wikipediaLink)
        {
            try
            {
                // Validate Wikipedia URL
                if (!ValidateWikipediaUrl(wikipediaLink))
                {
                    return BadRequest("Invalid Wikipedia URL");
                }

                // Make HTTP request to fetch Wikipedia content
                HttpResponseMessage response = await _httpClient.GetAsync(wikipediaLink);
                response.EnsureSuccessStatusCode();

                // Read HTML content and clean it
                string htmlContent = await response.Content.ReadAsStringAsync();
                string cleanedText = CleanWikipediaHtml(htmlContent);

                return Ok(cleanedText);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching Wikipedia content: {ex.Message}");
            }
        }

        private string CleanWikipediaHtml(string html)
        {
            // Use HtmlAgilityPack to parse and clean HTML
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove unwanted elements
            RemoveElements(doc, "//script");
            RemoveElements(doc, "//style");

            // Get the text content from the cleaned document
            string cleanedText = doc.DocumentNode.InnerText;

            // Remove blank lines from cleanedText
            cleanedText = RemoveBlankLines(cleanedText);

            return cleanedText;
        }

        private string RemoveBlankLines(string text)
        {
            // Split text into lines and filter out empty lines
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Join lines back into a single string
            string cleanedText = string.Join(Environment.NewLine, lines);

            return cleanedText;
        }

        private void RemoveElements(HtmlDocument doc, string xpath)
        {
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes != null)
            {
                foreach (HtmlNode node in nodes)
                {
                    node.Remove();
                }
            }
        }

        private bool ValidateWikipediaUrl(string wikipediaLink)
        {
            if (string.IsNullOrEmpty(wikipediaLink))
            {
                return false;
            }

            // Example validation: Check if the link starts with https://en.wikipedia.org/wiki/ or https://de.wikipedia.org/wiki/
            // Modify this logic based on your requirements
            if (!Regex.IsMatch(wikipediaLink, @"^https:\/\/[a-z]{2}\.wikipedia\.org\/wiki\/", RegexOptions.IgnoreCase))
            {
                return false;
            }

            // Add more validation as needed

            return true;
        }
    }
}