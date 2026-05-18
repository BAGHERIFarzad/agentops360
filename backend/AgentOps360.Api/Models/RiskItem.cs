namespace AgentOps360.Api.Models;

public class RiskItem
{
    public int Id { get; set; }

    public int AgentRunId { get; set; }

    public AgentRun? AgentRun { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Severity { get; set; } = "Medium";

    public string Mitigation { get; set; } = string.Empty;

    public string SourceEvidence { get; set; } = string.Empty;
}