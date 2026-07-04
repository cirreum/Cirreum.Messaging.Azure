# Backlog

Deferred work for **Cirreum.Messaging.Azure**. Items here are tracked but not yet ready
to ship — either because the cost outweighs the benefit in isolation, or
because they're waiting on a forcing function (a related change, a consumer
upgrade, a coordinated multi-repo rollout).

## How this file works

- Each item is a `###` heading so it can be linked to and parsed.
- Each item declares **`SemVer:`** (`Patch` | `Minor` | `Major` | `Unspecified`),
  **`Trigger:`** (the human-readable condition that will make it ready), and
  **`Noted:`** (the date the item was added).
- The Cirreum DevOps release scripts (`PatchRelease`, `MinorRelease`,
  `MajorRelease`) surface items at-or-below the requested bump level so the
  operator can decide whether to fold them in before tagging.
- Items that ship: move from this file to `docs/CHANGELOG.md` under
  `[Unreleased]`. Items that grow into design discussions: promote to an ADR.

## Queued

### Add a test suite

**SemVer:** Unspecified
**Trigger:** Next substantive (non-propagation) change to this repo, or a decision on the test harness shape.
**Noted:** 2026-07-04

The repo has no `tests/` folder. Pure unit coverage is possible for the
registrar/configuration/caching layers (settings binding, keyed registration,
sender/receiver cache expiration), but meaningful coverage of the client
surface needs a broker — evaluate the Azure Service Bus emulator
(`mcr.microsoft.com/azure-messaging/servicebus-emulator`) as an integration
harness versus mocking `ServiceBusClient` seams. Scaffold from
`DevOps\templates\tests\` (dedicated `tests/Cirreum.Messaging.Azure.Tests.slnx`,
never in the main slnx).

### Honor receiver tuning options (prefetch, lock renewal)

**SemVer:** Minor
**Trigger:** `Cirreum.Messaging` extends `IMessagingClient.UseQueueReceiver` / `UseSubscription` with receiver tuning parameters.
**Noted:** 2026-07-04

Mirror of the `Cirreum.Runtime.Messaging` backlog item: its
`ReceiverOptions.PrefetchCount` / `MaxAutoLockRenewalDuration` settings are
inert because the `IMessagingClient` factory methods take no tuning
parameters. When the contract grows them, this package maps the values onto
`ServiceBusReceiverOptions` / `ServiceBusProcessorOptions`.
