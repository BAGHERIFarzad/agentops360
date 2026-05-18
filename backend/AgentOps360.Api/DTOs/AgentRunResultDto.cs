namespace AgentOps360.Api.DTOs;

public class AgentRunResultDto
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string InputType { get; set; } = string.Empty;

    public string OriginalInput { get; set; } = string.Empty;

    public string Transcript { get; set; } = string.Empty;

    public string ExecutiveSummary { get; set; } = string.Empty;

    public string DecisionsJson { get; set; } = "[]";

    public string AgentTraceJson { get; set; } = "[]";

    public int ConfidenceScore { get; set; }

    public string AiMode { get; set; } = "Gemini Live";

    public DateTime CreatedAt { get; set; }

    public List<AgentTaskDto> Tasks { get; set; } = new();

    public List<RiskItemDto> Risks { get; set; } = new();

    public List<GeneratedEmailDto> GeneratedEmails { get; set; } = new();
}

public class AgentTaskDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Owner { get; set; }

    public string Priority { get; set; } = "Medium";

    public string Status { get; set; } = "Todo";

    public DateTime? DueDate { get; set; }

    public string SourceEvidence { get; set; } = string.Empty;
}

public class RiskItemDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Severity { get; set; } = "Medium";

    public string Mitigation { get; set; } = string.Empty;

    public string SourceEvidence { get; set; } = string.Empty;
}

public class GeneratedEmailDto
{
    public int Id { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Tone { get; set; } = "Professional";
}