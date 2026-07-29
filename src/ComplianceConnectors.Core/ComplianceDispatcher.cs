using Polly;
using Polly.Retry;

namespace ComplianceConnectors.Core;

public enum ReportStatus
{
    NotApplicable,
    Delivered,
    DeadLettered
}

public sealed record ReportOutcome(string RegimeCode, ReportStatus Status, int Attempts, string? Error = null);

/// <summary>
/// Stands in for the "Azure Service Bus topic" from the proposal: one document.signed event
/// fans out to every applicable reporter. Each reporter gets its own retry policy and failure
/// isolation, so a slow or flaky government API for one country can never block another country,
/// or the document flow itself.
/// </summary>
public sealed class ComplianceDispatcher
{
    private readonly IReadOnlyList<IComplianceReporter> _reporters;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly Action<string>? _log;

    public ComplianceDispatcher(IEnumerable<IComplianceReporter> reporters, Action<string>? log = null, int maxRetries = 3)
    {
        _reporters = reporters.ToList();
        _log = log;
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _log?.Invoke($"    retry {args.AttemptNumber + 1}/{maxRetries} after: {args.Outcome.Exception?.Message}");
                    return default;
                }
            })
            .Build();
    }

    public async Task<IReadOnlyList<ReportOutcome>> DispatchAsync(SignedDocument document, CancellationToken cancellationToken = default)
    {
        var tasks = _reporters.Select(reporter => HandleOneAsync(reporter, document, cancellationToken));
        return await Task.WhenAll(tasks);
    }

    private async Task<ReportOutcome> HandleOneAsync(IComplianceReporter reporter, SignedDocument document, CancellationToken ct)
    {
        if (!reporter.AppliesTo(document))
            return new ReportOutcome(reporter.RegimeCode, ReportStatus.NotApplicable, 0);

        var attempts = 0;
        try
        {
            await _retryPipeline.ExecuteAsync(async token =>
            {
                attempts++;
                await reporter.ReportAsync(document, token);
            }, ct);

            _log?.Invoke($"  [{reporter.RegimeCode}] delivered ({attempts} attempt{(attempts == 1 ? "" : "s")})");
            return new ReportOutcome(reporter.RegimeCode, ReportStatus.Delivered, attempts);
        }
        catch (Exception ex)
        {
            // Exhausted retries: the document itself stays valid — the report goes to a
            // dead-letter queue for follow-up instead of blocking the transport.
            _log?.Invoke($"  [{reporter.RegimeCode}] DEAD-LETTERED after {attempts} attempts: {ex.Message}");
            return new ReportOutcome(reporter.RegimeCode, ReportStatus.DeadLettered, attempts, ex.Message);
        }
    }
}
