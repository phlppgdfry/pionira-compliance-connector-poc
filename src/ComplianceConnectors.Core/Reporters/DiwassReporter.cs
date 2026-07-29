namespace ComplianceConnectors.Core.Reporters;

/// <summary>
/// EU DIWASS — cross-border green/amber-list waste shipments. Modelled as a freshly launched,
/// occasionally flaky government API: exactly the situation expected around a mandatory
/// EU-wide go-live, where every Member State starts registering in the same window.
/// </summary>
public sealed class DiwassReporter : IComplianceReporter
{
    private readonly Random _random;
    private int _callCount;

    public DiwassReporter(int? seed = null) => _random = seed.HasValue ? new Random(seed.Value) : new Random();

    public string RegimeCode => "EU-DIWASS";

    public bool AppliesTo(SignedDocument document) =>
        document.ContainsWaste && document.OriginCountry != document.DestinationCountry;

    public Task ReportAsync(SignedDocument document, CancellationToken cancellationToken)
    {
        _callCount++;
        // First two calls per process simulate the government API being overloaded at launch.
        if (_callCount <= 2 && _random.NextDouble() < 0.7)
            throw new HttpRequestException("DIWASS gateway timeout (503) — registration window overloaded");

        return Task.Delay(20, cancellationToken);
    }
}
