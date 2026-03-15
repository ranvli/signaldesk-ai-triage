namespace SignalDesk.DTOs;

public record StatsResponse(
    Dictionary<string, int> ByCategory,
    Dictionary<string, int> ByPriority);
