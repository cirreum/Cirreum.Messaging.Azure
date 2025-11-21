# Cirreum.Messaging.Azure

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Messaging.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Messaging.Azure/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Messaging.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Messaging.Azure/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Messaging.Azure?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Messaging.Azure/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Messaging.Azure?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Messaging.Azure/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Distributed messaging using Azure ServiceBus**

## Overview

**Cirreum.Messaging.Azure** provides a high-level abstraction over Azure Service Bus for distributed messaging scenarios. Built on the Cirreum Foundation Framework, it offers automatic service registration, intelligent caching, comprehensive health checks, and a clean API for queues, topics, and subscriptions.

## Features

- **Auto-Registration**: Automatic discovery and registration of messaging clients from configuration
- **Smart Caching**: Intelligent sender/receiver caching with sliding expiration and automatic cleanup
- **Health Monitoring**: Comprehensive health checks for queues, topics, and subscriptions with configurable caching
- **Clean Abstractions**: Unified messaging API that abstracts Azure Service Bus complexity
- **Production Ready**: Built-in retry policies, connection management, and telemetry integration

## Quick Start

### Installation

```bash
dotnet add package Cirreum.Messaging.Azure
```

### Basic Usage

```csharp
// Add to your Program.cs or Startup.cs
builder.AddAzureMessagingClient("default", connectionString);

// Inject and use
public class MessageService(IMessagingClient client)
{
    public async Task SendMessageAsync(string queueName, object message)
    {
        var queue = client.UseQueue(queueName);
        var outbound = new OutboundMessage(message);
        await queue.PublishMessageAsync(outbound);
    }
    
    public async Task<InboundMessage?> ReceiveMessageAsync(string queueName)
    {
        var queue = client.UseQueue(queueName);
        return await queue.ReceiveMessageAsync();
    }
}
```

### Configuration-Based Registration

```json
{
  "Messaging": {
    "Azure": {
      "Instances": {
        "primary": {
          "ConnectionString": "Endpoint=sb://...",
          "Name": "Primary Bus"
        }
      }
    }
  }
}
```

### Topics and Subscriptions

```csharp
// Publishing to topic
var topic = client.UseTopic("events");
await topic.BroadcastMessageAsync(message);

// Subscribing to topic
var subscription = client.UseSubscription("events", "processor");
var message = await subscription.ReceiveMessageAsync();
```

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

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*