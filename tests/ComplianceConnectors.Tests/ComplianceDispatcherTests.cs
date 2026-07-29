using ComplianceConnectors.Core;

namespace ComplianceConnectors.Tests;

public class ComplianceDispatcherTests
{
    private static SignedDocument SampleDocument(bool waste = true, string origin = "BE", string destination = "NL") =>
        new("DOC-1", DocumentType.AnnexVii, origin, destination, waste, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Skips_reporters_that_do_not_apply()
    {
        var dispatcher = new ComplianceDispatcher(new IComplianceReporter[] { new FakeReporter("X", Behavior.AlwaysSucceeds, appliesTo: false) });

        var outcomes = await dispatcher.DispatchAsync(SampleDocument());

        Assert.Equal(ReportStatus.NotApplicable, Assert.Single(outcomes).Status);
    }

    [Fact]
    public async Task Delivers_when_reporter_succeeds()
    {
        var dispatcher = new ComplianceDispatcher(new IComplianceReporter[] { new FakeReporter("X", Behavior.AlwaysSucceeds) });

        var outcome = Assert.Single(await dispatcher.DispatchAsync(SampleDocument()));

        Assert.Equal(ReportStatus.Delivered, outcome.Status);
        Assert.Equal(1, outcome.Attempts);
    }

    [Fact]
    public async Task Retries_transient_failures_then_delivers()
    {
        var flaky = new FakeReporter("X", Behavior.FailsTwiceThenSucceeds);
        var dispatcher = new ComplianceDispatcher(new IComplianceReporter[] { flaky }, maxRetries: 5);

        var outcome = Assert.Single(await dispatcher.DispatchAsync(SampleDocument()));

        Assert.Equal(ReportStatus.Delivered, outcome.Status);
        Assert.Equal(3, outcome.Attempts);
    }

    [Fact]
    public async Task Dead_letters_after_exhausting_retries_without_throwing()
    {
        var dispatcher = new ComplianceDispatcher(new IComplianceReporter[] { new FakeReporter("X", Behavior.AlwaysFails) }, maxRetries: 2);

        var outcome = Assert.Single(await dispatcher.DispatchAsync(SampleDocument()));

        Assert.Equal(ReportStatus.DeadLettered, outcome.Status);
        Assert.Equal(3, outcome.Attempts); // initial attempt + 2 retries
        Assert.NotNull(outcome.Error);
    }

    [Fact]
    public async Task One_reporter_failing_does_not_affect_another_reporters_outcome()
    {
        var failing = new FakeReporter("EU-DIWASS", Behavior.AlwaysFails);
        var healthy = new FakeReporter("BE-MATIS", Behavior.AlwaysSucceeds);
        var dispatcher = new ComplianceDispatcher(new IComplianceReporter[] { failing, healthy }, maxRetries: 1);

        var outcomes = await dispatcher.DispatchAsync(SampleDocument());

        Assert.Equal(ReportStatus.DeadLettered, outcomes.Single(o => o.RegimeCode == "EU-DIWASS").Status);
        Assert.Equal(ReportStatus.Delivered, outcomes.Single(o => o.RegimeCode == "BE-MATIS").Status);
    }

    private enum Behavior { AlwaysSucceeds, AlwaysFails, FailsTwiceThenSucceeds }

    private sealed class FakeReporter(string regimeCode, Behavior behavior, bool appliesTo = true) : IComplianceReporter
    {
        private int _calls;

        public string RegimeCode => regimeCode;

        public bool AppliesTo(SignedDocument document) => appliesTo;

        public Task ReportAsync(SignedDocument document, CancellationToken cancellationToken)
        {
            _calls++;
            var shouldFail = behavior switch
            {
                Behavior.AlwaysSucceeds => false,
                Behavior.AlwaysFails => true,
                Behavior.FailsTwiceThenSucceeds => _calls <= 2,
                _ => false
            };

            if (shouldFail)
                throw new InvalidOperationException($"simulated failure #{_calls}");

            return Task.CompletedTask;
        }
    }
}
