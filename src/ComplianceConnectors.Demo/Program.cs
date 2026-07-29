using ComplianceConnectors.Core;
using ComplianceConnectors.Core.Reporters;

Console.WriteLine("Compliance Connector Framework — demo");
Console.WriteLine("Simulates one document.signed event fanning out to independent country connectors.\n");

var dispatcher = new ComplianceDispatcher(
    new IComplianceReporter[]
    {
        new MatisReporter(),
        new DiwassReporter(seed: 7),
        new DecaReporter()
    },
    log: Console.WriteLine);

var documents = new[]
{
    new SignedDocument("DOC-BE-NL-001", DocumentType.AnnexVii, "BE", "NL", ContainsWaste: true, DateTimeOffset.UtcNow),
    new SignedDocument("DOC-BE-ES-002", DocumentType.ECmr, "BE", "ES", ContainsWaste: false, DateTimeOffset.UtcNow),
    new SignedDocument("DOC-ES-FR-003", DocumentType.AnnexVii, "ES", "FR", ContainsWaste: true, DateTimeOffset.UtcNow),
};

foreach (var doc in documents)
{
    Console.WriteLine($"\nSigned: {doc.DocumentId}  ({doc.OriginCountry} -> {doc.DestinationCountry}, waste={doc.ContainsWaste})");
    var outcomes = await dispatcher.DispatchAsync(doc);

    foreach (var outcome in outcomes.Where(o => o.Status != ReportStatus.NotApplicable))
        Console.WriteLine($"  => {outcome.RegimeCode}: {outcome.Status} ({outcome.Attempts} attempt(s))");

    // The key point: even if EU-DIWASS is dead-lettered after retries, the document itself
    // was signed and the transport proceeds. BE-MATIS and ES-DECA are entirely unaffected.
    Console.WriteLine(outcomes.Any(o => o.Status == ReportStatus.DeadLettered)
        ? "  Document remains valid — dead-lettered report queued for retry, transport is not blocked."
        : "  All applicable regimes reported successfully.");
}
