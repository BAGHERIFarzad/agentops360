namespace AgentOps360.Api.Models;

public class GeneratedEmail
{
    public int Id { get; set; }

    public int AgentRunId { get; set; }

    public AgentRun? AgentRun { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Tone { get; set; } = "Professional";
}