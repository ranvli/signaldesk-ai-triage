using SignalDesk.Domain.Entities;
using SignalDesk.Domain.Enums;

namespace SignalDesk.Infrastructure.Repositories;

public interface IFeedbackRepository
{
    void Add(FeedbackItem item);
    Task<IReadOnlyList<FeedbackItem>> GetAllAsync();
    Task<FeedbackItem?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
    Task<Dictionary<FeedbackCategory, int>> GetCategoryCountsAsync();
}
