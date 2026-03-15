namespace SignalDesk.Infrastructure.Services;

public interface IFeedbackAiService
{
    Task<AiAnalysisResult> AnalyzeAsync(string text);
}
