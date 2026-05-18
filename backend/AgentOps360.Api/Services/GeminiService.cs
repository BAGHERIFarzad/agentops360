using System.Text;
using System.Text.Json;

namespace AgentOps360.Api.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<GeminiAgentOutput?> AnalyzeMeetingAsync(string input)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is missing in appsettings.json.");
        }

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var prompt = BuildPrompt(input);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text = prompt
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        HttpResponseMessage? response = null;
        string responseContent = string.Empty;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            response = await _httpClient.SendAsync(request);
            responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"===== GEMINI RAW RESPONSE - ATTEMPT {attempt} =====");
            Console.WriteLine(responseContent);
            Console.WriteLine("===== END GEMINI RAW RESPONSE =====");

            if (response.IsSuccessStatusCode)
            {
                break;
            }

            if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
            {
                await Task.Delay(attempt * 2000);
                continue;
            }

            throw new Exception($"Gemini API error: {response.StatusCode} - {responseContent}");
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini API unavailable after retries: {response?.StatusCode} - {responseContent}");
        }

        using var document = JsonDocument.Parse(responseContent);

        var text = document
            .RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cleanedJson = CleanGeminiJson(text);

        Console.WriteLine("===== CLEANED GEMINI JSON =====");
        Console.WriteLine(cleanedJson);
        Console.WriteLine("===== END CLEANED GEMINI JSON =====");

        var output = JsonSerializer.Deserialize<GeminiAgentOutput>(
            cleanedJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        Console.WriteLine("===== DESERIALIZED GEMINI OUTPUT =====");
        Console.WriteLine($"Summary length: {output?.ExecutiveSummary?.Length}");
        Console.WriteLine($"Confidence: {output?.ConfidenceScore}");
        Console.WriteLine($"Decisions: {output?.Decisions?.Count}");
        Console.WriteLine($"Tasks: {output?.Tasks?.Count}");
        Console.WriteLine($"Risks: {output?.Risks?.Count}");
        Console.WriteLine($"Email subject: {output?.GeneratedEmail?.Subject}");
        Console.WriteLine($"Trace: {output?.AgentTrace?.Count}");
        Console.WriteLine("===== END DESERIALIZED GEMINI OUTPUT =====");

        return output;
    }

    private static string CleanGeminiJson(string text)
    {
        var cleaned = text.Trim();

        if (cleaned.StartsWith("```json"))
        {
            cleaned = cleaned.Replace("```json", "").Replace("```", "").Trim();
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Replace("```", "").Trim();
        }

        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');

        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return cleaned;
    }

    private static string BuildPrompt(string input)
    {
        return $$"""
You are AgentOps 360, an autonomous enterprise operations agent.

Analyze the following business meeting notes and return ONLY valid JSON.
Do not include markdown.
Do not include explanations outside JSON.

For dueDate, never use words like Tomorrow, Monday, next week.
Use ISO format YYYY-MM-DD when a date is clear.
Use null when the date is not clear.

Your job:
1. Summarize the meeting.
2. Extract business decisions.
3. Create concrete action tasks.
4. Detect risks.
5. Generate a professional follow-up email.
6. Create a multi-agent trace.
7. Give a confidence score from 0 to 100.

Required JSON schema:

{
  "executiveSummary": "string",
  "confidenceScore": 87,
  "decisions": [
    {
      "title": "string",
      "evidence": "string"
    }
  ],
  "tasks": [
    {
      "title": "string",
      "owner": "string",
      "priority": "High | Medium | Low",
      "status": "Todo",
      "dueDate": null,
      "sourceEvidence": "string"
    }
  ],
  "risks": [
    {
      "title": "string",
      "severity": "High | Medium | Low",
      "mitigation": "string",
      "sourceEvidence": "string"
    }
  ],
  "generatedEmail": {
    "subject": "string",
    "tone": "Professional",
    "body": "string"
  },
  "agentTrace": [
    {
      "agent": "Intake Agent",
      "step": "string",
      "status": "completed"
    },
    {
      "agent": "Reasoning Agent",
      "step": "string",
      "status": "completed"
    },
    {
      "agent": "Planning Agent",
      "step": "string",
      "status": "completed"
    },
    {
      "agent": "Communication Agent",
      "step": "string",
      "status": "completed"
    },
    {
      "agent": "Audit Agent",
      "step": "string",
      "status": "completed"
    }
  ]
}

Meeting notes:
{{input}}
""";
    }
}

public class GeminiAgentOutput
{
    public string ExecutiveSummary { get; set; } = string.Empty;

    public int ConfidenceScore { get; set; }

    public List<GeminiDecision> Decisions { get; set; } = new();

    public List<GeminiTask> Tasks { get; set; } = new();

    public List<GeminiRisk> Risks { get; set; } = new();

    public GeminiEmail GeneratedEmail { get; set; } = new();

    public List<GeminiTraceStep> AgentTrace { get; set; } = new();
}

public class GeminiDecision
{
    public string Title { get; set; } = string.Empty;

    public string Evidence { get; set; } = string.Empty;
}

public class GeminiTask
{
    public string Title { get; set; } = string.Empty;

    public string? Owner { get; set; }

    public string Priority { get; set; } = "Medium";

    public string Status { get; set; } = "Todo";

    public string? DueDate { get; set; }

    public string SourceEvidence { get; set; } = string.Empty;
}

public class GeminiRisk
{
    public string Title { get; set; } = string.Empty;

    public string Severity { get; set; } = "Medium";

    public string Mitigation { get; set; } = string.Empty;

    public string SourceEvidence { get; set; } = string.Empty;
}

public class GeminiEmail
{
    public string Subject { get; set; } = string.Empty;

    public string Tone { get; set; } = "Professional";

    public string Body { get; set; } = string.Empty;
}

public class GeminiTraceStep
{
    public string Agent { get; set; } = string.Empty;

    public string Step { get; set; } = string.Empty;

    public string Status { get; set; } = "completed";
}