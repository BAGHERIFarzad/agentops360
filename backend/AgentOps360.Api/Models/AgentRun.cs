namespace AgentOps360.Api.Models;

public class AgentRun
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    public string InputType { get; set; } = "text";

    public string OriginalInput { get; set; } = string.Empty;

    public string Transcript { get; set; } = string.Empty;

    public string ExecutiveSummary { get; set; } = string.Empty;

    public string DecisionsJson { get; set; } = "[]";

    public string AgentTraceJson { get; set; } = "[]";

    public int ConfidenceScore { get; set; }
    public string AiMode { get; set; } = "Gemini Live";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<AgentTask> Tasks { get; set; } = new();

    public List<RiskItem> Risks { get; set; } = new();

    public List<GeneratedEmail> GeneratedEmails { get; set; } = new();
}