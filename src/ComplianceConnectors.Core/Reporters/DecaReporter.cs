namespace ComplianceConnectors.Core.Reporters;

/// <summary>Spain's electronic control document (DeCA) — mandatory from October 2026. Modelled as
/// a new but so-far-stable regime, added without touching any other connector.</summary>
public sealed class DecaReporter : IComplianceReporter
{
    public string RegimeCode => "ES-DECA";

    public bool AppliesTo(SignedDocument document) =>
        document.OriginCountry == "ES" || document.DestinationCountry == "ES";

    public Task ReportAsync(SignedDocument document, CancellationToken cancellationToken) =>
        Task.Delay(15, cancellationToken);
}
