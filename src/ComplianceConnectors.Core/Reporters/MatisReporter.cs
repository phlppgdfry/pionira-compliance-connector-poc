namespace ComplianceConnectors.Core.Reporters;

/// <summary>
/// OVAM/MATIS — Flemish waste-transport reporting. Unlike DIWASS/DeCA, this regime is not a
/// live API today: eWastra generates a CSV export that a person submits to OVAM by hand. The
/// contract doesn't care — ReportAsync here stands in for "prepare and stage the export file"
/// rather than "call a government endpoint". Same interface, different delivery mechanism.
/// </summary>
public sealed class MatisReporter : IComplianceReporter
{
    public string RegimeCode => "BE-MATIS";

    public bool AppliesTo(SignedDocument document) =>
        document.ContainsWaste && (document.OriginCountry == "BE" || document.DestinationCountry == "BE");

    public Task ReportAsync(SignedDocument document, CancellationToken cancellationToken) =>
        Task.Delay(15, cancellationToken); // simulated: generating/staging the CSV export, not an API call
}
