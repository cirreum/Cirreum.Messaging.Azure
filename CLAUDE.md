# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

- **Build**: `dotnet build Cirreum.Messaging.Azure.slnx --configuration Release`
- **Restore**: `dotnet restore Cirreum.Messaging.Azure.slnx`  
- **Pack**: `dotnet pack Cirreum.Messaging.Azure.slnx --configuration Release --output ./artifacts`
- **Solution File**: `Cirreum.Messaging.Azure.slnx` (Visual Studio solution format)

## Project Structure

This is a .NET 10.0 class library that provides Azure Service Bus messaging capabilities for the Cirreum framework:

- `src/Cirreum.Messaging.Azure/` - Main library containing Azure Service Bus implementation
- `build/` - MSBuild configuration files for packaging, versioning, and CI/CD
- `.github/workflows/publish.yml` - Automated NuGet publishing workflow

## Architecture Overview

**Core Pattern**: Service Provider Registration with Auto-Discovery
- Uses `AzureServiceBusRegistrar` that implements the Cirreum ServiceProvider pattern
- Automatically registers messaging clients based on configuration sections
- Supports both manual registration via extension methods and auto-discovery

**Key Components**:
- `AzureServiceBusClient`: Main client wrapper with caching for senders/receivers (30min sliding expiration)
- `IMessagingClient` abstraction: Provides queue, topic, and subscription operations
- Health checks: Comprehensive validation for queues, topics, and subscriptions with configurable caching
- Configuration: `AzureServiceBusInstanceSettings` with connection strings and client options

**Messaging Patterns**:
- **Queues**: Point-to-point messaging via `IMessagingQueue`
- **Topics/Subscriptions**: Pub/sub messaging via `IMessagingTopicSender`/`IMessagingSubscriptionReceiver`
- **Message Types**: Unified `OutboundMessage` and `InboundMessage` abstractions

**Caching Strategy**: 
- Senders/receivers cached for 30 minutes with automatic disposal
- Health check results cached (60s success, 30s failure) with jitter
- Memory-efficient cleanup via post-eviction callbacks

## Dependencies

Core dependencies managed in `.csproj`:
- `Azure.Messaging.ServiceBus` (7.20.1) - Azure SDK
- `Azure.Identity` (1.17.1) - Authentication
- `Cirreum.Messaging` (1.0.102) - Core messaging abstractions  
- `Cirreum.ServiceProvider` (1.0.2) - Service provider registration pattern

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