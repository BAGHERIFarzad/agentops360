namespace AgentOps360.Api.DTOs;

public class FileUploadResultDto
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    public string ExtractedText { get; set; } = string.Empty;
}