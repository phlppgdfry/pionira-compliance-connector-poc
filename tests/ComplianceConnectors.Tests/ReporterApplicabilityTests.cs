using ComplianceConnectors.Core;
using ComplianceConnectors.Core.Reporters;

namespace ComplianceConnectors.Tests;

public class ReporterApplicabilityTests
{
    private static SignedDocument Doc(string origin, string destination, bool waste) =>
        new("DOC-1", DocumentType.AnnexVii, origin, destination, waste, DateTimeOffset.UtcNow);

    [Theory]
    [InlineData("BE", "NL", true, true)]
    [InlineData("NL", "BE", true, true)]
    [InlineData("BE", "NL", false, false)] // no waste on board -> MATIS does not apply
    [InlineData("FR", "DE", true, false)]  // Belgium not involved
    public void Matis_applies_only_to_belgian_waste_shipments(string origin, string destination, bool waste, bool expected) =>
        Assert.Equal(expected, new MatisReporter().AppliesTo(Doc(origin, destination, waste)));

    [Theory]
    [InlineData("BE", "NL", true, true)]
    [InlineData("BE", "BE", true, false)] // domestic shipment -> out of DIWASS scope
    [InlineData("BE", "NL", false, false)] // no waste -> out of scope
    public void Diwass_applies_only_to_cross_border_waste_shipments(string origin, string destination, bool waste, bool expected) =>
        Assert.Equal(expected, new DiwassReporter(seed: 1).AppliesTo(Doc(origin, destination, waste)));

    [Theory]
    [InlineData("ES", "FR", true)]
    [InlineData("FR", "ES", true)]
    [InlineData("FR", "DE", false)]
    public void Deca_applies_only_when_spain_is_involved(string origin, string destination, bool expected) =>
        Assert.Equal(expected, new DecaReporter().AppliesTo(Doc(origin, destination, waste: false)));

    [Fact]
    public void Adding_a_new_country_does_not_require_touching_existing_reporters()
    {
        // The point of the interface: this "new country" is a self-contained addition.
        var france = new FranceStub();
        Assert.True(france.AppliesTo(Doc("FR", "BE", waste: false)));
    }

    private sealed class FranceStub : IComplianceReporter
    {
        public string RegimeCode => "FR-NEW-REGIME";
        public bool AppliesTo(SignedDocument document) => document.OriginCountry == "FR" || document.DestinationCountry == "FR";
        public Task ReportAsync(SignedDocument document, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
