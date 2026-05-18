namespace AgentOps360.Api.DTOs;

public class CreateAgentRunRequest
{
    public int ProjectId { get; set; }

    public string InputType { get; set; } = "text";

    public string OriginalInput { get; set; } = string.Empty;
}