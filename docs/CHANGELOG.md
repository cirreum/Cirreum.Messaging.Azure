# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.1] - 2026-08-17

### Updated

- Updated NuGet packages.

## [1.2.0] - 2026-08-04

### Added

- **Receiver tuning reaches Service Bus.** The client implements the tuned
  `UseQueueReceiver(string, ReceiverTuning)` / `UseSubscription(string, string, ReceiverTuning)`
  overloads from `Cirreum.Messaging` 1.1.1, mapping `PrefetchCount` onto
  `ServiceBusReceiverOptions` at receiver creation. Receivers are cached by name, so the
  tuning recorded at a name's first request becomes that name's identity: a later request for
  the same receiver with *different* tuning throws instead of silently returning an instance
  tuned differently than asked. (From the backlog; middle rung of the three-repo
  receiver-tuning chain — `Cirreum.Runtime.Messaging` flows the configured values through in
  its own release.)

### Updated

- Re-pinned `Cirreum.Messaging` `1.1.0` → `1.1.1` (`ReceiverTuning` is creation-time
  broker options only).

## [1.1.0] - 2026-07-29

### Added

- **Configurable credentials for identity-based authentication** via the nested `Credential` block
  from `Cirreum.ServiceProvider` 1.1.0 (`Mode`: `Default` / `ManagedIdentity` / `Developer`, plus
  optional `IdentityId` selecting a user-assigned managed identity). The fully-qualified-namespace path
  previously hardcoded `new DefaultAzureCredential()` with no options — no tenant pinning, no
  identity selection.
- `Identifier` on the instance settings resolves as the Entra tenant, forwarded to every
  tenant-aware credential.
- A `Credential` block alongside a key-based connection string fails at startup with
  `InvalidOperationException` — identity configuration cannot apply to key authentication.
- An unrecognized `CredentialMode` value fails at startup instead of silently degrading to the
  default chain.

### Updated

- Updated NuGet packages.

## [1.0.23] - 2026-07-20

### Updated

- Updated NuGet packages.

## [1.0.22] - 2026-07-19

### Updated

- Updated NuGet packages.

## [1.0.20] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.19] - 2026-07-04

### Fixed

- **Queue health-check error detail was always masked** — on a queue check failure outside Production, the health-check data was populated with the exception message and then immediately overwritten with the generic `"Failed"` marker (a missing early-return; the topic and subscription checks behaved correctly). Non-production environments now surface the actual error detail, matching the other check types. Found by the repo's first test suite (`tests/Cirreum.Messaging.Azure.Tests`, 27 tests), added alongside this release.
- **Package description corrected** — the NuGet `<Description>` claimed "Distributed messaging using Azure ServiceBus"; this package is the Azure Service Bus **provider** for the Cirreum Messaging broker abstractions (`IMessagingClient` — queues, topics, subscriptions). The distributed-messaging model and delivery engine live in `Cirreum.Messaging.Distributed` and `Cirreum.Runtime.Messaging`.
- **README rewritten to the shipped surface** — the previous Quick Start documented a registration and usage flow that didn't match the package: it now covers configuration-based registration under `Cirreum:Messaging:Providers:Azure` (keyed `IMessagingClient` per instance), the connection-resolution order (`Name` via `ConnectionStrings`/Key Vault → inline `ConnectionString` → fully qualified namespace with `DefaultAzureCredential`), the manual `AddAzureMessagingClient` overloads, and the real send/receive/ack model.

## [1.0.18] - 2026-05-07

### Updated

- Updated NuGet packages.

## [1.0.17] - 2026-05-01

### Updated

- Updated NuGet packages.
