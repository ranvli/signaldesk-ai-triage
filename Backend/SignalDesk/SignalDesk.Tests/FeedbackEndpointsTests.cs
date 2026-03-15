using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalDesk.Domain.Enums;
using SignalDesk.DTOs;
using SignalDesk.Infrastructure.Data;
using SignalDesk.Infrastructure.Services;
using Xunit;

namespace SignalDesk.Tests;

public class FeedbackEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FeedbackEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostFeedback_ReturnsCreated_WithAiFields()
    {
        var response = await _client.PostAsJsonAsync("/feedback",
            new CreateFeedbackRequest("The app crashes on login"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FeedbackResponse>();
        Assert.NotNull(body);
        Assert.Equal("The app crashes on login", body.Text);
        Assert.Equal("Open", body.Status);
        Assert.NotEmpty(body.Summary);
        Assert.NotEmpty(body.Category);
        Assert.NotEmpty(body.Priority);
    }

    [Fact]
    public async Task GetFeedback_ReturnsItems()
    {
        await _client.PostAsJsonAsync("/feedback", new CreateFeedbackRequest("Feedback A"));

        var response = await _client.GetAsync("/feedback");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<FeedbackResponse[]>();
        Assert.NotNull(items);
        Assert.True(items.Length >= 1);
    }

    [Fact]
    public async Task PatchAction_SetsStatusToActioned()
    {
        var created = await PostAndReadAsync("Please add dark mode");

        var patch = await _client.PatchAsync($"/feedback/{created.Id}/action", null);
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var updated = await patch.Content.ReadFromJsonAsync<FeedbackResponse>();
        Assert.Equal("Actioned", updated!.Status);
    }

    [Fact]
    public async Task PatchDismiss_SetsStatusToDismissed()
    {
        var created = await PostAndReadAsync("Duplicate report");

        var patch = await _client.PatchAsync($"/feedback/{created.Id}/dismiss", null);
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var updated = await patch.Content.ReadFromJsonAsync<FeedbackResponse>();
        Assert.Equal("Dismissed", updated!.Status);
    }

    [Fact]
    public async Task PatchAction_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PatchAsync($"/feedback/{Guid.NewGuid()}/action", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_ReturnsCategoryAndPriorityCounts()
    {
        await _client.PostAsJsonAsync("/feedback", new CreateFeedbackRequest("Some feedback"));

        var response = await _client.GetAsync("/stats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<StatsResponse>();
        Assert.NotNull(stats);
        Assert.NotEmpty(stats.ByCategory);
        Assert.NotEmpty(stats.ByPriority);
    }

    private async Task<FeedbackResponse> PostAndReadAsync(string text)
    {
        var response = await _client.PostAsJsonAsync("/feedback", new CreateFeedbackRequest(text));
        return (await response.Content.ReadFromJsonAsync<FeedbackResponse>())!;
    }
}

// ── Test infrastructure ──────────────────────────────────────────────────────

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SignalDeskDbContext>));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);

            services.AddDbContext<SignalDeskDbContext>(opts =>
                opts.UseSqlite(_connection));

            var aiDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IFeedbackAiService));
            if (aiDescriptor is not null) services.Remove(aiDescriptor);

            services.AddScoped<IFeedbackAiService, FakeFeedbackAiService>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<SignalDeskDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

internal sealed class FakeFeedbackAiService : IFeedbackAiService
{
    public Task<AiAnalysisResult> AnalyzeAsync(string text) =>
        Task.FromResult(new AiAnalysisResult(
            Summary: $"Summary of: {text}",
            Category: FeedbackCategory.Bug,
            Priority: FeedbackPriority.Medium));
}
