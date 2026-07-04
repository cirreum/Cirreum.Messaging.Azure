# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

- **Build**: `dotnet build Cirreum.Messaging.Azure.slnx --configuration Release`
- **Restore**: `dotnet restore Cirreum.Messaging.Azure.slnx`
- **Pack**: `dotnet pack Cirreum.Messaging.Azure.slnx --configuration Release --output ./artifacts`
- **Solution File**: `Cirreum.Messaging.Azure.slnx` (Visual Studio solution format)

## Project Structure

This is a .NET 10.0 class library — the Azure Service Bus provider for the Cirreum Messaging track (implements the `Cirreum.Messaging` broker abstractions):

- `src/Cirreum.Messaging.Azure/` - Main library containing the Azure Service Bus implementation
- `build/` - MSBuild configuration files for packaging, versioning, and CI/CD
- `docs/` - CHANGELOG (Keep a Changelog 1.1.0) and BACKLOG (deferred work)
- `.github/workflows/publish.yml` - Automated NuGet publishing workflow

## Architecture Overview

**Core Pattern**: Service Provider Registration with Auto-Discovery
- `AzureServiceBusRegistrar` implements the Cirreum ServiceProvider pattern
- Instances configured under `Cirreum:Messaging:Providers:Azure:Instances` are auto-registered as **keyed** `IMessagingClient` services (DI key = instance key)
- Manual registration also available via the `AddAzureMessagingClient` extension overloads
- Connection resolution order: `Name` via `ConnectionStrings` (incl. Key Vault-backed config) → inline `ConnectionString` → fully qualified namespace value connects with `DefaultAzureCredential`

**Key Components**:
- `AzureServiceBusClient`: Main client wrapper with caching for senders/receivers (30min sliding expiration)
- `IMessagingClient` abstraction (from `Cirreum.Messaging`): queue, topic, and subscription factories plus `UseClient<T>` native escape hatch
- Health checks: validation for queues, topics, and subscriptions with configurable caching
- Configuration: `AzureServiceBusInstanceSettings` with connection settings and client options

**Messaging Patterns**:
- **Queues**: Point-to-point via `UseQueue` / `UseQueueSender` / `UseQueueReceiver`
- **Topics/Subscriptions**: Pub/sub via `UseTopic` / `UseSubscription`
- **Message model**: `OutboundMessage` on the send side; on the receive side the `AzureServiceBus*ReceivedMessage` / `*PeekedMessage` wrappers expose content, properties, and the broker ack model (`Complete` / `Abandon` / `Defer` / `DeadLetter` / `RenewLock`)

**Caching Strategy**:
- Senders/receivers cached for 30 minutes with automatic disposal
- Health check results cached (60s success, 30s failure) with jitter
- Memory-efficient cleanup via post-eviction callbacks

## Dependencies

Managed in `src/Cirreum.Messaging.Azure/Cirreum.Messaging.Azure.csproj` (see the csproj for current versions):

- `Azure.Messaging.ServiceBus` - Azure SDK
- `Azure.Identity` - Authentication (`DefaultAzureCredential` for namespace connections)
- `Cirreum.Messaging` - Broker abstractions this package implements
- `Cirreum.ServiceProvider` - Service provider registration pattern

## Code Conventions

Based on `.editorconfig`:
- **Target Framework**: .NET 10.0 with latest C# language version
- **Nullable**: Enabled throughout
- **Tabs**: 4-space indentation with tabs
- **Usings**: Inside namespace, implicit usings enabled
- **Naming**: PascalCase for types/methods, interfaces prefixed with 'I'
- **Properties**: this. qualification required for properties/methods/events

## Build Configuration

- **CI Detection**: Automatically detects Azure DevOps, GitHub Actions, or generic CI
- **Local Development**: Uses version 1.0.100-rc by default
- **Packaging**: Configured for NuGet with SourceLink, icon, and metadata
- **InternalsVisibleTo**: Test projects have access to internals (local builds only)
