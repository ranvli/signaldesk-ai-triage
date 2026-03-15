using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SignalDesk.Domain.Enums;
using SignalDesk.Infrastructure.Services;
using Xunit;

namespace SignalDesk.Tests;

public class OllamaFeedbackAiServiceTests
{
    // A realistic short input — under 120 chars so Truncate() returns it unchanged
    private const string Input = "The checkout button does nothing when I click it.";

    // ── helpers ──────────────────────────────────────────────────────────────

    private static OllamaFeedbackAiService BuildService(
        string innerJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        // Wrap the AI text in the Ollama envelope: { "response": "..." }
        var body = JsonSerializer.Serialize(new { response = innerJson });
        var handler = new StubHttpMessageHandler(body, statusCode);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var options = Options.Create(new OllamaOptions { Model = "test-model" });
        return new OllamaFeedbackAiService(client, options);
    }

    // ── fallback: invalid category ────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_UnrecognisedCategory_FallsBackToComplaint()
    {
        var aiJson = """{"summary": "Checkout is broken", "category": "unknown_type", "priority": "high"}""";
        var service = BuildService(aiJson);

        var result = await service.AnalyzeAsync(Input);

        Assert.Equal(FeedbackCategory.Complaint, result.Category);
    }

    [Fact]
    public async Task AnalyzeAsync_UnrecognisedCategory_StillParsesPriority()
    {
        var aiJson = """{"summary": "Checkout is broken", "category": "unknown_type", "priority": "high"}""";
        var service = BuildService(aiJson);

        var result = await service.AnalyzeAsync(Input);

        // Valid priority alongside an invalid category should still be honoured
        Assert.Equal(FeedbackPriority.High, result.Priority);
    }

    // ── fallback: malformed JSON ──────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_MalformedJson_FallsBackToComplaint()
    {
        var service = BuildService("Sorry, I cannot help with that.");

        var result = await service.AnalyzeAsync(Input);

        Assert.Equal(FeedbackCategory.Complaint, result.Category);
    }

    [Fact]
    public async Task AnalyzeAsync_MalformedJson_FallsBackToMediumPriority()
    {
        var service = BuildService("not json at all");

        var result = await service.AnalyzeAsync(Input);

        Assert.Equal(FeedbackPriority.Medium, result.Priority);
    }

    [Fact]
    public async Task AnalyzeAsync_MalformedJson_SummaryIsTruncatedInput()
    {
        var service = BuildService("not json at all");

        var result = await service.AnalyzeAsync(Input);

        // Input is under 120 chars, so the full text should be used as the summary
        Assert.Equal(Input, result.Summary);
    }

    [Fact]
    public async Task AnalyzeAsync_MalformedJson_LongInput_SummaryIsTruncatedTo120()
    {
        var longInput = new string('x', 200);
        var service = BuildService("not json at all");

        var result = await service.AnalyzeAsync(longInput);

        // Truncate() produces at most 120 chars + the ellipsis character
        Assert.Equal(120 + "…".Length, result.Summary.Length);
        Assert.EndsWith("…", result.Summary);
    }

    // ── fallback: HTTP error ──────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_OllamaReturnsHttpError_FallsBackToDefaults()
    {
        var service = BuildService(string.Empty, HttpStatusCode.InternalServerError);

        var result = await service.AnalyzeAsync(Input);

        Assert.Equal(FeedbackCategory.Complaint, result.Category);
        Assert.Equal(FeedbackPriority.Medium, result.Priority);
        Assert.Equal(Input, result.Summary);
    }

    // ── happy path: snake_case category mapping ───────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_FeatureRequestSnakeCase_MapsToEnum()
    {
        // The system prompt instructs the model to return "feature_request".
        // Enum.TryParse alone would fail; the explicit _categoryMap must handle it.
        var aiJson = """{"summary": "User wants dark mode", "category": "feature_request", "priority": "low"}""";
        var service = BuildService(aiJson);

        var result = await service.AnalyzeAsync(Input);

        Assert.Equal(FeedbackCategory.FeatureRequest, result.Category);
        Assert.Equal(FeedbackPriority.Low, result.Priority);
        Assert.Equal("User wants dark mode", result.Summary);
    }
}

// ── test double ───────────────────────────────────────────────────────────────

internal sealed class StubHttpMessageHandler(
    string responseBody,
    HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        });
}
