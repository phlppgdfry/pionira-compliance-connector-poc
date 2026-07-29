namespace ComplianceConnectors.Core;

/// <summary>
/// One national or EU reporting regime (MATIS, DIWASS, DeCA, ...).
/// Adding a new country means implementing this contract once — nothing else in the
/// document-signing flow changes, and no other regime is affected.
/// </summary>
public interface IComplianceReporter
{
    /// <summary>Short code identifying the regime, e.g. "BE-MATIS", "EU-DIWASS", "ES-DECA".</summary>
    string RegimeCode { get; }

    /// <summary>Whether this regime needs to be notified for the given document.</summary>
    bool AppliesTo(SignedDocument document);

    /// <summary>Report the document to the regime's own system. May throw on transient failure.</summary>
    Task ReportAsync(SignedDocument document, CancellationToken cancellationToken);
}
