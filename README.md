# Compliance Connector Framework — proof of concept

A small, runnable .NET solution proving out the architecture proposed in the
[Compliance Connector Framework pitch](https://phlppgdfry.github.io/pionira-compliance-pitch/),
written for Pionira's Senior Software Engineer .NET vacancy.

## The idea

A transport document reaches a point in its lifecycle (created, signed, ...) → one
`SignedDocument` event → every applicable national or EU compliance regime
(`IComplianceReporter`) picks it up independently, with its own retry policy. A flaky or
newly-launched government API (like DIWASS around its mandatory EU go-live) can never block
another regime or the document itself. Not every regime delivers the same way, either —
`ReportAsync` stands for "handle this regime's reporting", whether that's a live API call
(DIWASS, DeCA) or generating an export file (MATIS, today).

## What's here

- **`ComplianceConnectors.Core`** — the contract (`IComplianceReporter`), the domain event
  (`SignedDocument`), and the `ComplianceDispatcher` that fans one event out to many reporters
  with per-reporter retry (via Polly) and failure isolation.
- **`Reporters/`** — three example connectors: `MatisReporter` (Belgium/OVAM — today a CSV
  export submitted by hand, not a live API), `DiwassReporter` (EU, simulated as flaky around
  launch), `DecaReporter` (Spain, mandatory October 2026). Adding a fourth regime means adding
  a fourth class — nothing else changes.
- **`ComplianceConnectors.Demo`** — a console app that signs a few sample documents and prints
  what each connector does, including a live retry against the simulated DIWASS outage.
- **`ComplianceConnectors.Tests`** — 16 xUnit tests: applicability rules per regime, retry
  behavior, dead-lettering after exhausted retries, and — the architectural point — proof that
  one reporter failing never affects another reporter's outcome.

## Run it

```bash
dotnet test                                    # 16 tests, all green
dotnet run --project src/ComplianceConnectors.Demo
```

## What this deliberately does not do

This is a proof of concept, not production code: no real Azure Service Bus connection, no real
DIWASS/DeCA/MATIS API calls, no persistence or audit trail. Those are the natural next steps —
this exists to prove the shape of the architecture is sound before spending real integration
time on it.
