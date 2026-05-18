using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AgentOps360.Api.Services;

public class SpeechmaticsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SpeechmaticsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> TranscribeAudioAsync(IFormFile file)
    {
        var apiKey = _configuration["Speechmatics:ApiKey"];
        var baseUrl = _configuration["Speechmatics:BaseUrl"]
            ?? "https://eu1.asr.api.speechmatics.com/v2";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Speechmatics API key is missing in appsettings.json.");
        }

        var jobId = await CreateTranscriptionJobAsync(file, apiKey, baseUrl);
        await WaitForJobCompletionAsync(jobId, apiKey, baseUrl);

        var transcript = await GetTranscriptAsync(jobId, apiKey, baseUrl);

        if (string.IsNullOrWhiteSpace(transcript))
        {
            throw new Exception("Speechmatics returned an empty transcript.");
        }

        return transcript;
    }

    private async Task<string> CreateTranscriptionJobAsync(
        IFormFile file,
        string apiKey,
        string baseUrl)
    {
        await using var fileStream = file.OpenReadStream();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/jobs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var config = new
        {
            type = "transcription",
            transcription_config = new
            {
                language = "en",
                operating_point = "enhanced",
                diarization = "speaker",
                enable_entities = true
            }
        };

        var configJson = JsonSerializer.Serialize(config);

        using var multipartContent = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType
        );

        multipartContent.Add(fileContent, "data_file", file.FileName);
        multipartContent.Add(new StringContent(configJson, Encoding.UTF8, "application/json"), "config");

        request.Content = multipartContent;

        using var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine("===== SPEECHMATICS CREATE JOB RESPONSE =====");
        Console.WriteLine(responseContent);
        Console.WriteLine("===== END SPEECHMATICS CREATE JOB RESPONSE =====");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Speechmatics create job failed: {response.StatusCode} - {responseContent}");
        }

        using var document = JsonDocument.Parse(responseContent);

        if (document.RootElement.TryGetProperty("id", out var idElement))
        {
            return idElement.GetString()
                   ?? throw new Exception("Speechmatics returned an empty job id.");
        }

        if (document.RootElement.TryGetProperty("job", out var jobElement) &&
            jobElement.TryGetProperty("id", out var jobIdElement))
        {
            return jobIdElement.GetString()
                   ?? throw new Exception("Speechmatics returned an empty job id.");
        }

        throw new Exception("Could not find Speechmatics job id in response.");
    }

    private async Task WaitForJobCompletionAsync(
        string jobId,
        string apiKey,
        string baseUrl)
    {
        const int maxAttempts = 40;
        const int delayMs = 3000;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/jobs/{jobId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"===== SPEECHMATICS JOB STATUS ATTEMPT {attempt} =====");
            Console.WriteLine(responseContent);
            Console.WriteLine("===== END SPEECHMATICS JOB STATUS =====");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Speechmatics job status failed: {response.StatusCode} - {responseContent}");
            }

            using var document = JsonDocument.Parse(responseContent);

            var job = document.RootElement.GetProperty("job");
            var status = job.GetProperty("status").GetString();

            if (string.Equals(status, "done", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Speechmatics rejected the job: {responseContent}");
            }

            await Task.Delay(delayMs);
        }

        throw new TimeoutException("Speechmatics transcription timed out. Try a shorter audio file.");
    }

    private async Task<string> GetTranscriptAsync(
        string jobId,
        string apiKey,
        string baseUrl)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/jobs/{jobId}/transcript?format=txt"
        );

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request);
        var transcript = await response.Content.ReadAsStringAsync();

        Console.WriteLine("===== SPEECHMATICS TRANSCRIPT RESPONSE =====");
        Console.WriteLine(transcript);
        Console.WriteLine("===== END SPEECHMATICS TRANSCRIPT RESPONSE =====");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Speechmatics transcript download failed: {response.StatusCode} - {transcript}");
        }

        return transcript;
    }
}