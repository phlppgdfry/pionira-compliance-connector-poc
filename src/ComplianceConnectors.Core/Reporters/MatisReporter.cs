namespace ComplianceConnectors.Core.Reporters;

/// <summary>OVAM/MATIS — Flemish waste-transport reporting. Modelled as the stable, existing regime.</summary>
public sealed class MatisReporter : IComplianceReporter
{
    public string RegimeCode => "BE-MATIS";

    public bool AppliesTo(SignedDocument document) =>
        document.ContainsWaste && (document.OriginCountry == "BE" || document.DestinationCountry == "BE");

    public Task ReportAsync(SignedDocument document, CancellationToken cancellationToken) =>
        Task.Delay(15, cancellationToken); // stable, low-latency API in this simulation
}
