namespace SignalDesk.DTOs;

public record FeedbackResponse(
    Guid Id,
    string Text,
    string Summary,
    string Category,
    string Status,
    string Priority,
    DateTime CreatedAt);
