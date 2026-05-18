using System.Text;
using AgentOps360.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;
using AgentOps360.Api.Services;

namespace AgentOps360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    [HttpPost("text")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<FileUploadResultDto>> UploadTextFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension != ".txt")
        {
            return BadRequest("Only .txt files are supported by this endpoint.");
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var extractedText = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return BadRequest("The uploaded file is empty.");
        }

        return Ok(new FileUploadResultDto
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            ExtractedText = extractedText
        });
    }

    [HttpPost("pdf")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<FileUploadResultDto>> UploadPdfFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension != ".pdf")
        {
            return BadRequest("Only .pdf files are supported by this endpoint.");
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        var extractedTextBuilder = new StringBuilder();

        using (var document = PdfDocument.Open(memoryStream))
        {
            foreach (var page in document.GetPages())
            {
                var text = page.Text;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    extractedTextBuilder.AppendLine($"--- Page {page.Number} ---");
                    extractedTextBuilder.AppendLine(text);
                    extractedTextBuilder.AppendLine();
                }
            }
        }

        var extractedText = extractedTextBuilder.ToString();

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return BadRequest("No readable text was found in this PDF. It may be scanned/image-based.");
        }

        return Ok(new FileUploadResultDto
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            ExtractedText = extractedText
        });
    }
    
    [HttpPost("audio")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<FileUploadResultDto>> UploadAudioFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No audio file uploaded.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var allowedExtensions = new[]
        {
            ".mp3", ".wav", ".m4a", ".mp4", ".webm", ".ogg", ".flac"
        };

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Unsupported audio format. Please upload mp3, wav, m4a, mp4, webm, ogg, or flac.");
        }

        try
        {
            var transcript = await _speechmaticsService.TranscribeAudioAsync(file);

            return Ok(new FileUploadResultDto
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                ExtractedText = transcript
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Audio transcription failed.",
                error = ex.Message
            });
        }
    }
    private readonly SpeechmaticsService _speechmaticsService;

        public UploadsController(SpeechmaticsService speechmaticsService)
        {
            _speechmaticsService = speechmaticsService;
        }
}