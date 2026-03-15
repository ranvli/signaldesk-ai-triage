using SignalDesk.Domain.Entities;
using SignalDesk.Domain.Enums;
using SignalDesk.DTOs;
using SignalDesk.Infrastructure.Repositories;
using SignalDesk.Infrastructure.Services;

namespace SignalDesk.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/feedback").WithTags("Feedback");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", GetAllAsync);
        group.MapPatch("/{id:guid}/action", ActionAsync);
        group.MapPatch("/{id:guid}/dismiss", DismissAsync);

        app.MapGet("/stats", GetStatsAsync).WithTags("Stats");
    }

    private static async Task<IResult> CreateAsync(
        CreateFeedbackRequest request,
        IFeedbackRepository repo,
        IFeedbackAiService ai)
    {
        var analysis = await ai.AnalyzeAsync(request.Text);

        var item = new FeedbackItem
        {
            Id = Guid.NewGuid(),
            Text = request.Text,
            Summary = analysis.Summary,
            Category = analysis.Category,
            Priority = analysis.Priority,
            Status = FeedbackStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        repo.Add(item);
        await repo.SaveChangesAsync();

        return Results.Created($"/feedback/{item.Id}", item.ToResponse());
    }

    private static async Task<IResult> GetAllAsync(IFeedbackRepository repo) =>
        Results.Ok((await repo.GetAllAsync()).Select(f => f.ToResponse()));

    private static Task<IResult> ActionAsync(Guid id, IFeedbackRepository repo) =>
        SetStatusAsync(id, FeedbackStatus.Actioned, repo);

    private static Task<IResult> DismissAsync(Guid id, IFeedbackRepository repo) =>
        SetStatusAsync(id, FeedbackStatus.Dismissed, repo);

    private static async Task<IResult> SetStatusAsync(
        Guid id, FeedbackStatus status, IFeedbackRepository repo)
    {
        var item = await repo.GetByIdAsync(id);
        if (item is null) return Results.NotFound();

        item.Status = status;
        await repo.SaveChangesAsync();

        return Results.Ok(item.ToResponse());
    }

    private static async Task<IResult> GetStatsAsync(IFeedbackRepository repo)
    {
        var items = await repo.GetAllAsync();

        return Results.Ok(new StatsResponse(
            ByCategory: Enum.GetValues<FeedbackCategory>()
                .ToDictionary(c => c.ToString(), c => items.Count(i => i.Category == c)),
            ByPriority: Enum.GetValues<FeedbackPriority>()
                .ToDictionary(p => p.ToString(), p => items.Count(i => i.Priority == p))));
    }
}

internal static class FeedbackItemExtensions
{
    internal static FeedbackResponse ToResponse(this FeedbackItem item) => new(
        item.Id,
        item.Text,
        item.Summary,
        item.Category.ToString(),
        item.Status.ToString(),
        item.Priority.ToString(),
        item.CreatedAt);
}
