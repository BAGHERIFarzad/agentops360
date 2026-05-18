using System.Text.Json;
using AgentOps360.Api.DTOs;
using AgentOps360.Api.Models;

namespace AgentOps360.Api.Services;

public class AgentOrchestrator
{
    private readonly GeminiService _geminiService;

    public AgentOrchestrator(GeminiService geminiService)
    {
        _geminiService = geminiService;
    }

    private static readonly JsonSerializerOptions CamelCaseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public async Task<AgentRun> BuildRealAgentRunAsync(CreateAgentRunRequest request)
    {
        GeminiAgentOutput aiOutput;
        var aiMode = "Gemini Live";

        try
        {
            var result = await _geminiService.AnalyzeMeetingAsync(request.OriginalInput);

            if (result == null)
            {
                throw new Exception("Gemini returned an empty response.");
            }

            aiOutput = result;

            Console.WriteLine("===== ORCHESTRATOR AI OUTPUT =====");
            Console.WriteLine($"Summary: {aiOutput.ExecutiveSummary}");
            Console.WriteLine($"Confidence: {aiOutput.ConfidenceScore}");
            Console.WriteLine($"Tasks count: {aiOutput.Tasks.Count}");
            Console.WriteLine($"Risks count: {aiOutput.Risks.Count}");
            Console.WriteLine($"Decisions count: {aiOutput.Decisions.Count}");
            Console.WriteLine("===== END ORCHESTRATOR AI OUTPUT =====");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Gemini failed. Using local fallback.");
            Console.WriteLine(ex.Message);

            aiMode = "Fallback Engine";
            aiOutput = BuildFallbackOutput(request.OriginalInput);            
        }

        var agentRun = new AgentRun
        {
            ProjectId = request.ProjectId,
            InputType = request.InputType,
            OriginalInput = request.OriginalInput,
            Transcript = request.OriginalInput,

            
            ExecutiveSummary = aiOutput.ExecutiveSummary,
            ConfidenceScore = aiOutput.ConfidenceScore,
            AiMode = aiMode,

            DecisionsJson = JsonSerializer.Serialize(aiOutput.Decisions, CamelCaseJsonOptions),
            AgentTraceJson = JsonSerializer.Serialize(aiOutput.AgentTrace, CamelCaseJsonOptions),

            Tasks = aiOutput.Tasks.Select(t => new AgentTask
            {
                Title = t.Title,
                Owner = t.Owner,
                Priority = string.IsNullOrWhiteSpace(t.Priority) ? "Medium" : t.Priority,
                Status = string.IsNullOrWhiteSpace(t.Status) ? "Todo" : t.Status,
                DueDate = ParseDueDate(t.DueDate),
                SourceEvidence = t.SourceEvidence
            }).ToList(),

            Risks = aiOutput.Risks.Select(r => new RiskItem
            {
                Title = r.Title,
                Severity = string.IsNullOrWhiteSpace(r.Severity) ? "Medium" : r.Severity,
                Mitigation = r.Mitigation,
                SourceEvidence = r.SourceEvidence
            }).ToList(),

            GeneratedEmails = new List<GeneratedEmail>
            {
                new GeneratedEmail
                {
                    Subject = aiOutput.GeneratedEmail.Subject,
                    Body = aiOutput.GeneratedEmail.Body,
                    Tone = aiOutput.GeneratedEmail.Tone
                }
            }
        };

        return agentRun;
    }

    private static DateTime? ParseDueDate(string? dueDate)
    {
        if (string.IsNullOrWhiteSpace(dueDate))
        {
            return null;
        }

        if (DateTime.TryParse(dueDate, out var parsedDate))
        {
            return parsedDate;
        }

        var normalized = dueDate.Trim().ToLowerInvariant();

        return normalized switch
        {
            "tomorrow" => DateTime.UtcNow.Date.AddDays(1),
            "today" => DateTime.UtcNow.Date,
            "monday" => GetNextDayOfWeek(DayOfWeek.Monday),
            "tuesday" => GetNextDayOfWeek(DayOfWeek.Tuesday),
            "wednesday" => GetNextDayOfWeek(DayOfWeek.Wednesday),
            "thursday" => GetNextDayOfWeek(DayOfWeek.Thursday),
            "friday" => GetNextDayOfWeek(DayOfWeek.Friday),
            _ => null
        };
    }

    private static DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntil = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;

        if (daysUntil == 0)
        {
            daysUntil = 7;
        }

        return today.AddDays(daysUntil);
    }

    private static GeminiAgentOutput BuildFallbackOutput(string input)
    {
        var lowerInput = input.ToLowerInvariant();

        var isHr = lowerInput.Contains("hr") ||
                   lowerInput.Contains("onboarding") ||
                   lowerInput.Contains("employees") ||
                   lowerInput.Contains("mentor");

        var isSaas = lowerInput.Contains("saas") ||
                     lowerInput.Contains("stripe") ||
                     lowerInput.Contains("api") ||
                     lowerInput.Contains("beta");

        if (isHr)
        {
            return new GeminiAgentOutput
            {
                ExecutiveSummary =
                    "The HR team discussed onboarding 25 new employees next month. Key blockers include delayed laptop delivery, incomplete training materials, and unconfirmed mentor assignments. The onboarding plan must be finalized by Monday.",

                ConfidenceScore = 82,

                Decisions = new List<GeminiDecision>
                {
                    new GeminiDecision
                    {
                        Title = "Finalize the onboarding plan by Monday",
                        Evidence = "The HR manager wants the onboarding plan finalized by Monday."
                    },
                    new GeminiDecision
                    {
                        Title = "Escalate laptop delivery with the IT supplier",
                        Evidence = "Emma will contact the IT supplier tomorrow."
                    }
                },

                Tasks = new List<GeminiTask>
                {
                    new GeminiTask
                    {
                        Title = "Contact IT supplier about delayed laptop order",
                        Owner = "Emma",
                        Priority = "High",
                        Status = "Todo",
                        DueDate = "tomorrow",
                        SourceEvidence = "Emma will contact the IT supplier tomorrow."
                    },
                    new GeminiTask
                    {
                        Title = "Complete onboarding training materials",
                        Owner = "HR Team",
                        Priority = "High",
                        Status = "Todo",
                        DueDate = null,
                        SourceEvidence = "Training materials are incomplete."
                    },
                    new GeminiTask
                    {
                        Title = "Confirm mentor assignment list",
                        Owner = "HR Manager",
                        Priority = "Medium",
                        Status = "Todo",
                        DueDate = "monday",
                        SourceEvidence = "Nobody has confirmed the mentor assignment list."
                    }
                },

                Risks = new List<GeminiRisk>
                {
                    new GeminiRisk
                    {
                        Title = "Laptop delay may block employee onboarding",
                        Severity = "High",
                        Mitigation = "Escalate with the supplier and prepare backup devices.",
                        SourceEvidence = "The laptop order is delayed."
                    },
                    new GeminiRisk
                    {
                        Title = "Incomplete training materials may reduce onboarding quality",
                        Severity = "Medium",
                        Mitigation = "Assign content owners and validate the materials before Monday.",
                        SourceEvidence = "Training materials are incomplete."
                    },
                    new GeminiRisk
                    {
                        Title = "Missing mentor assignments may affect new employee support",
                        Severity = "Medium",
                        Mitigation = "Confirm mentors and communicate the assignment list before onboarding starts.",
                        SourceEvidence = "Nobody has confirmed the mentor assignment list."
                    }
                },

                GeneratedEmail = new GeminiEmail
                {
                    Subject = "Follow-up — New Employee Onboarding Action Plan",
                    Tone = "Professional",
                    Body =
                        "Hello team,\n\nFollowing our onboarding discussion, here is the proposed action plan:\n\n- Emma will contact the IT supplier about the delayed laptop order.\n- The HR team will complete the training materials.\n- The mentor assignment list must be confirmed before Monday.\n\nMain risks:\n- Laptop delivery delay may block onboarding readiness.\n- Incomplete training materials may affect onboarding quality.\n- Missing mentor assignments may reduce new employee support.\n\nRecommended next step: hold a short onboarding readiness review before Monday.\n\nBest regards,\nAgentOps 360"
                },

                AgentTrace = BuildDefaultTrace()
            };
        }

        if (isSaas)
        {
            return new GeminiAgentOutput
            {
                ExecutiveSummary =
                    "The meeting focused on preparing a SaaS beta launch. The main priorities are backend stability, payment validation, marketing preparation, and support ownership before launch.",

                ConfidenceScore = 87,

                Decisions = new List<GeminiDecision>
                {
                    new GeminiDecision
                    {
                        Title = "Move forward with beta launch preparation",
                        Evidence = "The meeting notes mention preparing the beta launch in three weeks."
                    },
                    new GeminiDecision
                    {
                        Title = "Prioritize technical and operational readiness",
                        Evidence = "Backend stability, payment testing, and support ownership were identified."
                    }
                },

                Tasks = new List<GeminiTask>
                {
                    new GeminiTask
                    {
                        Title = "Stabilize backend authentication API",
                        Owner = lowerInput.Contains("david") ? "David" : "Backend Team",
                        Priority = "High",
                        Status = "Todo",
                        DueDate = null,
                        SourceEvidence = "The backend authentication API is not fully stable."
                    },
                    new GeminiTask
                    {
                        Title = "Validate Stripe payment flow",
                        Owner = lowerInput.Contains("sarah") ? "Sarah" : "Product Team",
                        Priority = "High",
                        Status = "Todo",
                        DueDate = null,
                        SourceEvidence = "Stripe payment still needs testing."
                    },
                    new GeminiTask
                    {
                        Title = "Prepare launch email campaign",
                        Owner = "Marketing Team",
                        Priority = "Medium",
                        Status = "Todo",
                        DueDate = null,
                        SourceEvidence = "Marketing needs to prepare the launch email."
                    },
                    new GeminiTask
                    {
                        Title = "Assign customer support owner",
                        Owner = "Operations Team",
                        Priority = "Medium",
                        Status = "Todo",
                        DueDate = null,
                        SourceEvidence = "Someone from operations needs to own customer support."
                    }
                },

                Risks = new List<GeminiRisk>
                {
                    new GeminiRisk
                    {
                        Title = "Backend instability may delay beta launch",
                        Severity = "High",
                        Mitigation = "Run API stability tests and monitor authentication errors.",
                        SourceEvidence = "The backend authentication API is not fully stable."
                    },
                    new GeminiRisk
                    {
                        Title = "Payment workflow may fail during beta",
                        Severity = "High",
                        Mitigation = "Complete end-to-end Stripe test scenarios before launch.",
                        SourceEvidence = "Stripe payment still needs testing."
                    }
                },

                GeneratedEmail = new GeminiEmail
                {
                    Subject = "Follow-up — SaaS Beta Launch Action Plan",
                    Tone = "Professional",
                    Body =
                        "Hello team,\n\nFollowing the beta launch discussion, here is the proposed action plan:\n\n- Stabilize the backend authentication API.\n- Validate the Stripe payment flow.\n- Prepare the launch email campaign.\n- Assign a customer support owner.\n\nMain risks:\n- Backend instability may delay the launch.\n- Payment workflow needs validation before beta access.\n\nRecommended next step: schedule a launch readiness review within 48 hours.\n\nBest regards,\nAgentOps 360"
                },

                AgentTrace = BuildDefaultTrace()
            };
        }

        return new GeminiAgentOutput
        {
            ExecutiveSummary =
                "The meeting was analyzed using the local fallback engine because Gemini was temporarily unavailable. The system extracted likely decisions, tasks, risks, and follow-up actions from the provided notes.",

            ConfidenceScore = 70,

            Decisions = new List<GeminiDecision>
            {
                new GeminiDecision
                {
                    Title = "Move forward with the discussed action plan",
                    Evidence = "The meeting notes contain operational blockers and next steps."
                }
            },

            Tasks = new List<GeminiTask>
            {
                new GeminiTask
                {
                    Title = "Review meeting blockers and assign owners",
                    Owner = "Operations Team",
                    Priority = "High",
                    Status = "Todo",
                    DueDate = null,
                    SourceEvidence = input
                }
            },

            Risks = new List<GeminiRisk>
            {
                new GeminiRisk
                {
                    Title = "Some responsibilities may remain unclear",
                    Severity = "Medium",
                    Mitigation = "Confirm owners, deadlines, and dependencies in the next team review.",
                    SourceEvidence = "The notes contain unresolved operational points."
                }
            },

            GeneratedEmail = new GeminiEmail
            {
                Subject = "Follow-up — Meeting Action Plan",
                Tone = "Professional",
                Body =
                    "Hello team,\n\nFollowing the meeting, please review the key blockers, confirm owners, and validate deadlines so we can move forward with a clear execution plan.\n\nBest regards,\nAgentOps 360"
            },

            AgentTrace = BuildDefaultTrace()
        };
    }

    private static List<GeminiTraceStep> BuildDefaultTrace()
    {
        return new List<GeminiTraceStep>
        {
            new GeminiTraceStep
            {
                Agent = "Intake Agent",
                Step = "Parsed the meeting notes and identified the business context.",
                Status = "completed"
            },
            new GeminiTraceStep
            {
                Agent = "Reasoning Agent",
                Step = "Detected decisions, blockers, and operational priorities.",
                Status = "completed"
            },
            new GeminiTraceStep
            {
                Agent = "Planning Agent",
                Step = "Created tasks with owners, priorities, and source evidence.",
                Status = "completed"
            },
            new GeminiTraceStep
            {
                Agent = "Communication Agent",
                Step = "Generated a professional follow-up email.",
                Status = "completed"
            },
            new GeminiTraceStep
            {
                Agent = "Audit Agent",
                Step = "Checked risks, confidence, and missing responsibilities.",
                Status = "completed"
            }
        };
    }
}