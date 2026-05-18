namespace AgentOps360.Api.Models;

public class AgentTask
{
    public int Id { get; set; }

    public int AgentRunId { get; set; }

    public AgentRun? AgentRun { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Owner { get; set; }

    public string Priority { get; set; } = "Medium";

    public string Status { get; set; } = "Todo";

    public DateTime? DueDate { get; set; }

    public string SourceEvidence { get; set; } = string.Empty;
}