using SignalDesk.Domain.Enums;

namespace SignalDesk.Domain.Entities;

public class FeedbackItem
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public FeedbackCategory Category { get; set; }
    public FeedbackStatus Status { get; set; }
    public FeedbackPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}
