using SignalDesk.Domain.Enums;

namespace SignalDesk.Infrastructure.Services;

public record AiAnalysisResult(
    string Summary,
    FeedbackCategory Category,
    FeedbackPriority Priority);
