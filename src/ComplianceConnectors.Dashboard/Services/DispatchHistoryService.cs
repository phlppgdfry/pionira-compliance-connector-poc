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
}
