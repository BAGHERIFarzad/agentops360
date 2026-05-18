using AgentOps360.Api.Data;
using AgentOps360.Api.DTOs;
using AgentOps360.Api.Models;
using AgentOps360.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentOps360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentRunsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AgentOrchestrator _orchestrator;

    public AgentRunsController(AppDbContext context, AgentOrchestrator orchestrator)
    {
        _context = context;
        _orchestrator = orchestrator;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AgentRunResultDto>> GetAgentRun(int id)
    {
        var agentRun = await _context.AgentRuns
            .Include(a => a.Tasks)
            .Include(a => a.Risks)
            .Include(a => a.GeneratedEmails)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (agentRun == null)
        {
            return NotFound();
        }

        return Ok(ToDto(agentRun));
    }

    [HttpGet("project/{projectId:int}")]
    public async Task<ActionResult<List<AgentRunResultDto>>> GetAgentRunsByProject(int projectId)
    {
        var runs = await _context.AgentRuns
            .Where(a => a.ProjectId == projectId)
            .Include(a => a.Tasks)
            .Include(a => a.Risks)
            .Include(a => a.GeneratedEmails)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var result = runs.Select(ToDto).ToList();

        return Ok(result);
    }

    [HttpPost("run")]
    public async Task<ActionResult<AgentRunResultDto>> RunAgent(CreateAgentRunRequest request)
    {
        if (request.ProjectId <= 0)
        {
            return BadRequest("ProjectId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OriginalInput))
        {
            return BadRequest("OriginalInput is required.");
        }

        var projectExists = await _context.Projects
            .AnyAsync(p => p.Id == request.ProjectId);

        if (!projectExists)
        {
            return NotFound("Project not found.");
        }

        try
        {
            var agentRun = await _orchestrator.BuildRealAgentRunAsync(request);

            _context.AgentRuns.Add(agentRun);
            await _context.SaveChangesAsync();

            return Ok(ToDto(agentRun));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Agent run failed.",
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    private static AgentRunResultDto ToDto(AgentRun agentRun)
    {
        return new AgentRunResultDto
        {
            Id = agentRun.Id,
            ProjectId = agentRun.ProjectId,
            InputType = agentRun.InputType,
            OriginalInput = agentRun.OriginalInput,
            Transcript = agentRun.Transcript,
            ExecutiveSummary = agentRun.ExecutiveSummary,
            DecisionsJson = agentRun.DecisionsJson,
            AgentTraceJson = agentRun.AgentTraceJson,
            ConfidenceScore = agentRun.ConfidenceScore,
            AiMode = agentRun.AiMode,
            CreatedAt = agentRun.CreatedAt,

            Tasks = agentRun.Tasks.Select(t => new AgentTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Owner = t.Owner,
                Priority = t.Priority,
                Status = t.Status,
                DueDate = t.DueDate,
                SourceEvidence = t.SourceEvidence
            }).ToList(),

            Risks = agentRun.Risks.Select(r => new RiskItemDto
            {
                Id = r.Id,
                Title = r.Title,
                Severity = r.Severity,
                Mitigation = r.Mitigation,
                SourceEvidence = r.SourceEvidence
            }).ToList(),

            GeneratedEmails = agentRun.GeneratedEmails.Select(e => new GeneratedEmailDto
            {
                Id = e.Id,
                Subject = e.Subject,
                Body = e.Body,
                Tone = e.Tone
            }).ToList()
        };
    }
}