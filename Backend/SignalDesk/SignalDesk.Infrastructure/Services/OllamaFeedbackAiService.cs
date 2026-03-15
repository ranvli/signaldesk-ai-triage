using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SignalDesk.Domain.Enums;

namespace SignalDesk.Infrastructure.Services;

public class OllamaFeedbackAiService(HttpClient httpClient, IOptions<OllamaOptions> options)
    : IFeedbackAiService
{
    private readonly string _model = options.Value.Model;

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private const string SystemPrompt = """
        You are a customer feedback triage assistant for an internal SaaS tool.

        Your task:
        - summarize the feedback in one short sentence
        - classify it into exactly one category
        - assign a priority

        Allowed categories:
        - bug
        - feature_request
        - complaint
        - praise

        Allowed priorities:
        - low
        - medium
        - high

        Rules:
        - return valid JSON only
        - do not include markdown
        - do not include explanation
        - if the text reports broken behavior, errors, crashes, failed payments, or unexpected results, prefer bug
        - if the text asks for a new capability or improvement, prefer feature_request
        - if the text expresses dissatisfaction without a concrete new feature request, prefer complaint
        - if the text is mainly positive feedback, prefer praise

        Return this exact schema:
        {
          "summary": "string",
          "category": "bug | feature_request | complaint | praise",
          "priority": "low | medium | high"
        }
        """;

    // Maps snake_case values returned by the model to C# enum names
    private static readonly Dictionary<string, string> _categoryMap = new()
    {
        ["bug"]             = nameof(FeedbackCategory.Bug),
        ["feature_request"] = nameof(FeedbackCategory.FeatureRequest),
        ["complaint"]       = nameof(FeedbackCategory.Complaint),
        ["praise"]          = nameof(FeedbackCategory.Praise),
    };

    public async Task<AiAnalysisResult> AnalyzeAsync(string text)
    {
        var request = new OllamaRequest(
            Model:  _model,
            System: SystemPrompt,
            Prompt: text,
            Stream: false);

        try
        {
            var httpResponse = await httpClient.PostAsJsonAsync("/api/generate", request);
            httpResponse.EnsureSuccessStatusCode();

            var ollamaResponse = await httpResponse.Content.ReadFromJsonAsync<OllamaResponse>();
            var raw = ollamaResponse?.Response?.Trim() ?? string.Empty;

            raw = StripCodeFences(raw);

            var parsed = JsonSerializer.Deserialize<OllamaAiResult>(raw, _jsonOptions);

            if (parsed is null)
                return Fallback(text);

            var categoryName = _categoryMap.GetValueOrDefault(
                parsed.Category?.ToLowerInvariant() ?? string.Empty,
                parsed.Category ?? string.Empty);

            var category = Enum.TryParse<FeedbackCategory>(categoryName, ignoreCase: true, out var c)
                ? c
                : FeedbackCategory.Complaint;

            var priority = Enum.TryParse<FeedbackPriority>(parsed.Priority, ignoreCase: true, out var p)
                ? p
                : FeedbackPriority.Medium;

            return new AiAnalysisResult(
                Summary:  parsed.Summary ?? Truncate(text),
                Category: category,
                Priority: priority);
        }
        catch
        {
            return Fallback(text);
        }
    }

    private static AiAnalysisResult Fallback(string text) => new(
        Summary:  Truncate(text),
        Category: FeedbackCategory.Complaint,
        Priority: FeedbackPriority.Medium);

    private static string Truncate(string text, int max = 120) =>
        text.Length <= max ? text : text[..max] + "…";

    private static string StripCodeFences(string raw)
    {
        if (!raw.StartsWith("```"))
            return raw;

        var firstNewline = raw.IndexOf('\n');
        if (firstNewline < 0)
            return raw;

        raw = raw[(firstNewline + 1)..];

        var lastFence = raw.LastIndexOf("```");
        if (lastFence >= 0)
            raw = raw[..lastFence];

        return raw.Trim();
    }

    // ── Ollama HTTP DTOs (implementation detail) ─────────────────────────────

    private record OllamaRequest(
        [property: JsonPropertyName("model")]  string Model,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream);

    private record OllamaResponse(
        [property: JsonPropertyName("response")] string? Response);

    private record OllamaAiResult(
        string? Summary,
        string? Category,
        string? Priority);
}

