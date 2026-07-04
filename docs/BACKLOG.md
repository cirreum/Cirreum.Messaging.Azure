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

### Integration coverage via the Service Bus emulator

**SemVer:** Unspecified  
**Trigger:** A broker-behavior bug that unit tests can't reproduce, or CI capacity for container-based tests.  
**Noted:** 2026-07-04  

The unit suite (`tests/Cirreum.Messaging.Azure.Tests`, added 2026-07-04) covers
message mapping, client caching, health checks, and DI composition — all
broker-free. End-to-end send/receive/ack coverage of the
`AzureServiceBus*` sender/receiver wrappers needs a live broker; evaluate the
Azure Service Bus emulator
(`mcr.microsoft.com/azure-messaging/servicebus-emulator`) as the harness.

### Honor receiver tuning options (prefetch, lock renewal)

**SemVer:** Major  
**Trigger:** `Cirreum.Messaging` extends `IMessagingClient.UseQueueReceiver` / `UseSubscription` with receiver tuning parameters.  
**Noted:** 2026-07-04  

> SemVer is deliberately marked Major so this cross-repo item only surfaces at
> major releases — the change itself is additive (Minor) at this layer, but it
> cannot start until the upstream contract moves, so surfacing it on every
> patch/minor is noise.

Mirror of the `Cirreum.Runtime.Messaging` backlog item: its
`ReceiverOptions.PrefetchCount` / `MaxAutoLockRenewalDuration` settings are
inert because the `IMessagingClient` factory methods take no tuning
parameters. When the contract grows them, this package maps the values onto
`ServiceBusReceiverOptions` / `ServiceBusProcessorOptions`.
