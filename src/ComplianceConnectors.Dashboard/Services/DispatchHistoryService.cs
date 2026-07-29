using ComplianceConnectors.Core;
using ComplianceConnectors.Core.Reporters;

namespace ComplianceConnectors.Dashboard.Services;

public sealed record DispatchRecord(SignedDocument Document, IReadOnlyList<ReportOutcome> Outcomes, DateTimeOffset DispatchedAtUtc);

/// <summary>
/// Wraps the same ComplianceDispatcher from the console demo behind a small in-memory
/// history, so the Blazor UI can show — visually, not just in console output — what happens
/// to a document once it fans out to every applicable compliance connector.
/// </summary>
public sealed class DispatchHistoryService
{
    private readonly ComplianceDispatcher _dispatcher;
    private readonly List<DispatchRecord> _history = [];

    public DispatchHistoryService()
    {
        _dispatcher = new ComplianceDispatcher(
        [
            new MatisReporter(),
            new DiwassReporter(),
            new DecaReporter()
        ]);
    }

    public IReadOnlyList<DispatchRecord> History => _history;

    public event Action? OnChange;

    public async Task DispatchAsync(SignedDocument document)
    {
        var outcomes = await _dispatcher.DispatchAsync(document);
        _history.Insert(0, new DispatchRecord(document, outcomes, DateTimeOffset.UtcNow));
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _history.Clear();
        OnChange?.Invoke();
    }

    /// <summary>Per-regime counters across the whole session, for the summary cards.</summary>
    public IReadOnlyList<RegimeSummary> Summaries =>
        _history
            .SelectMany(r => r.Outcomes)
            .Where(o => o.Status != ReportStatus.NotApplicable)
            .GroupBy(o => o.RegimeCode)
            .Select(g => new RegimeSummary(
                g.Key,
                g.Count(o => o.Status == ReportStatus.Delivered),
                g.Count(o => o.Status == ReportStatus.DeadLettered),
                g.Sum(o => o.Attempts)))
            .OrderBy(s => s.RegimeCode)
            .ToList();
}

public sealed record RegimeSummary(string RegimeCode, int Delivered, int DeadLettered, int TotalAttempts);
