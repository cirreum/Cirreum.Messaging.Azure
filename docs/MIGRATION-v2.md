# Cirreum.Messaging.Azure v1 → v2 Migration

v2 tracks the property-ownership split in Cirreum.Messaging 2.0.0. This package implements the
abstraction; it adds no surface of its own, so the migration for an application is the rename
described in that package's [v2 migration](https://github.com/cirreum/Cirreum.Messaging/blob/main/docs/MIGRATION-v2.md).

What changes here is provider behaviour.

---

## Application properties are no longer filtered

v1 discarded any application property whose key matched a first-class Service Bus field name:

```csharp
public static string[] StandardProps = [
    nameof(ServiceBusMessage.MessageId),
    nameof(ServiceBusMessage.CorrelationId),
    nameof(ServiceBusMessage.ContentType),
    nameof(ServiceBusMessage.Subject),
    nameof(ServiceBusMessage.TimeToLive),
    nameof(ServiceBusMessage.ReplyTo)
];
```

The drop was silent — no exception, no log. Service Bus keeps application properties in a namespace
separate from those fields, so the collision the filter avoided does not exist on the wire; it was
removing data to prevent a non-problem.

In v2 those properties travel. An application that set `ApplicationProperties["Subject"]` and saw
nothing arrive will now see it arrive. `StandardProps` is removed.

A null-valued application property is still not transmitted, and this is now stated on the
abstraction rather than being a liberty this provider took.

## System properties share the wire dictionary

Service Bus exposes one application-property dictionary, so both bags travel in it. The reserved
`cirreum.` prefix splits them back out on receive: a reserved key becomes a `SystemProperties`
entry, everything else an `ApplicationProperties` entry. No key appears in both.

Keeping system properties in the same wire dictionary preserves broker-side subscription filters —
a rule filtering on `cirreum.identifier` continues to work unchanged.

A property written into `ApplicationProperties` under a reserved key is **not** forwarded. The
reserved space belongs to the framework, and an application cannot occupy it.

## Reading a system value

Inbound system values are read through `ToString()` rather than a type test, so a broker returning
another representation of a value cannot produce a silent miss. Values are surfaced as `string`.

## Batch sends

`ToAzureMessages(commonProperties)` is now `ToAzureMessages(commonApplicationProperties)`. Common
properties fill in only where the message did not set the key itself — unchanged in intent, but v1
skipped them entirely when a message carried no properties at all, which was backwards.
