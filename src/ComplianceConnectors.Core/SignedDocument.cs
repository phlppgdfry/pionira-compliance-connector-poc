namespace ComplianceConnectors.Core;

public enum DocumentType
{
    ECmr,
    Digid,
    AnnexVii
}

/// <summary>
/// The single event every connector reacts to: a transport document has just been signed.
/// This is the only thing the document-signing flow needs to know about compliance reporting.
/// </summary>
public sealed record SignedDocument(
    string DocumentId,
    DocumentType Type,
    string OriginCountry,
    string DestinationCountry,
    bool ContainsWaste,
    DateTimeOffset SignedAtUtc);
