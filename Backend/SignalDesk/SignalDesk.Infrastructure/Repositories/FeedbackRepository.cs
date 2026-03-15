using Microsoft.EntityFrameworkCore;
using SignalDesk.Domain.Entities;
using SignalDesk.Domain.Enums;
using SignalDesk.Infrastructure.Data;

namespace SignalDesk.Infrastructure.Repositories;

public class FeedbackRepository(SignalDeskDbContext db) : IFeedbackRepository
{
    public void Add(FeedbackItem item) =>
        db.FeedbackItems.Add(item);

    public async Task<IReadOnlyList<FeedbackItem>> GetAllAsync() =>
        await db.FeedbackItems
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<FeedbackItem?> GetByIdAsync(Guid id) =>
        await db.FeedbackItems.FindAsync(id);

    public Task SaveChangesAsync() =>
        db.SaveChangesAsync();

    public async Task<Dictionary<FeedbackCategory, int>> GetCategoryCountsAsync()
    {
        var counts = await db.FeedbackItems
            .GroupBy(f => f.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        return Enum.GetValues<FeedbackCategory>()
            .ToDictionary(
                c => c,
                c => counts.FirstOrDefault(x => x.Category == c)?.Count ?? 0);
    }
}
