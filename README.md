# Cirreum.Messaging.Azure

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Messaging.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Messaging.Azure/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Messaging.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Messaging.Azure/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Messaging.Azure?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Messaging.Azure/releases)
[![License](https://img.shields.io/badge/license-MIT-F2F2F2?style=flat-square&labelColor=1F1F1F)](https://github.com/cirreum/Cirreum.Messaging.Azure/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Azure Service Bus provider for the Cirreum Messaging track — implements the `IMessagingClient` broker abstractions.**

## Overview

**Cirreum.Messaging.Azure** provides a high-level abstraction over Azure Service Bus for distributed messaging scenarios. Built on the Cirreum Foundation Framework, it offers automatic service registration, intelligent caching, comprehensive health checks, and a clean API for queues, topics, and subscriptions.

## Features

- **Auto-Registration**: Automatic discovery and registration of messaging clients from configuration
- **Smart Caching**: Intelligent sender/receiver caching with sliding expiration and automatic cleanup
- **Health Monitoring**: Comprehensive health checks for queues, topics, and subscriptions with configurable caching
- **Clean Abstractions**: Unified messaging API that abstracts Azure Service Bus complexity
- **Faithful Property Mapping**: Application and system properties travel verbatim, split back apart on receive, with nothing silently dropped
- **Production Ready**: Built-in retry policies, connection management, and telemetry integration

## Quick Start

### Installation

```bash
dotnet add package Cirreum.Messaging.Azure
```

### Configuration-Based Registration

Instances are configured under `Cirreum:Messaging:Providers:Azure` and auto-registered by the Cirreum runtime (`builder.AddMessaging()` from `Cirreum.Runtime.Messaging`, or `RegisterServiceProvider<AzureServiceBusRegistrar, ...>` directly). Each entry under `Instances` becomes a **keyed** `IMessagingClient` registration whose DI key is the instance key:

```json
{
  "Cirreum": {
	"Messaging": {
	  "Providers": {
		"Azure": {
		  "Tracing": true,
		  "Instances": {
			"primary": {
			  "Name": "app-messaging-servicebus",
			  "HealthChecks": true
			}
		  }
		}
	  }
	}
  }
}
```

The connection is resolved from `Name` via `ConnectionStrings` (including Key Vault-backed configuration) first, falling back to an inline `ConnectionString` property on the instance. A fully qualified namespace value (e.g., `mybus.servicebus.windows.net`) connects with `DefaultAzureCredential` (managed identity) instead of a shared access key.

### Manual Registration

```csharp
// Explicit connection string
builder.AddAzureMessagingClient("primary", connectionString);

// Or settings callback
builder.AddAzureMessagingClient("primary", settings => {
	settings.Name = "app-messaging-servicebus";
	settings.HealthChecks = true;
});
```

### Basic Usage

Resolve the client by its instance key and use the queue/topic factories:

```csharp
public sealed class OrderQueueService(
	[FromKeyedServices("primary")] IMessagingClient client) {

	public Task SendAsync(Order order, CancellationToken ct) =>
		client.UseQueueSender("orders.pending.v1")
			.PublishMessageAsync(
				OutboundMessage.AsJsonContent(order).WithSubject("orders.created"),
				ct);

	public async Task ProcessOneAsync(CancellationToken ct) {
		var received = await client.UseQueueReceiver("orders.pending.v1")
			.ReceiveMessageAsync(cancellationToken: ct);
		var order = JsonSerializer.Deserialize<Order>(received.ContentString);
		// ... handle ...
		await received.CompleteMessageAsync(ct); // or Abandon / Defer / DeadLetter
	}
}
```

`UseQueue(name)` returns a combined sender/receiver when a service works both sides of one queue; `UseClient<ServiceBusClient>(...)` escape-hatches to the native SDK for operations outside the abstraction.

### Topics and Subscriptions

```csharp
// Publishing to a topic
await client.UseTopic("app.notifications.v1")
	.BroadcastMessageAsync(OutboundMessage.AsJsonContent(notice).WithSubject("notice.raised"));

// Consuming from a subscription
var received = await client.UseSubscription("app.notifications.v1", "api-head")
	.ReceiveMessageAsync();
```

## How properties map to Service Bus

Service Bus exposes a single application-property dictionary on the wire, so this provider merges
`ApplicationProperties` and `SystemProperties` onto it when sending and splits them back apart when
receiving, keyed by the reserved `cirreum.` prefix. No key ever appears in both bags.

Sharing the wire dictionary is deliberate rather than a compromise: a broker-side subscription
filter on a system property such as `cirreum.identifier` keeps working exactly as a filter on an
application property does.

Three behaviours are worth knowing:

- **Nothing is dropped for colliding with a message field.** A property keyed `Subject`,
  `MessageId`, `CorrelationId`, `ContentType`, `TimeToLive` or `ReplyTo` travels as an application
  property. Those live in a namespace separate from the first-class fields on the wire, so there is
  no collision to prevent. (Releases before 2.0.0 discarded them silently.)
- **A reserved key is not forwarded from the application bag.** Writing
  `ApplicationProperties["cirreum.node"]` has no effect on the wire — the reserved space belongs to
  the framework, and an application cannot forge a value in it.
- **A system value is read back through `ToString()`, not a type test.** The contract is that it is
  a string; reading it that way means a value the broker returns in another representation cannot
  produce a silent miss.

A null-valued application property is not transmitted — the one case where a property is dropped,
and it is stated on the abstraction rather than left to this provider.

## Identity-Based Authentication

Set the connection value to the fully qualified namespace (for example `{namespace}.servicebus.windows.net`) instead of an `Endpoint=...` connection string and the provider authenticates with Entra. The nested `Credential` block (shared across Cirreum providers) selects how:

```json
"Credential": { "Mode": "ManagedIdentity", "IdentityId": "<user-assigned-client-id>" }
```

- **Default** — `DefaultAzureCredential`; `IdentityId` pins the chain's managed-identity leg
- **ManagedIdentity** — deterministic `ManagedIdentityCredential`; omit `IdentityId` for system-assigned
- **Developer** — Visual Studio → Azure CLI → Azure PowerShell, as the signed-in developer

`Identifier` sets the Entra tenant for the tenant-aware credentials. Omitting the block entirely
means `Default`. A `Credential` block alongside a key-based connection string fails at startup —
identity configuration cannot apply to key authentication. The identity needs the service's
data-plane RBAC role (for example *Azure Service Bus Data Sender* / *Data Receiver*).

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Messaging.Azure follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*