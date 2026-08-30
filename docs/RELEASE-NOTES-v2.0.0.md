# Cirreum.Messaging.Azure 2.0.0

Implements the property-ownership split introduced in Cirreum.Messaging 2.0.0, and stops filtering
application properties.

## The split on the wire

Service Bus exposes one application-property dictionary, so `ApplicationProperties` and
`SystemProperties` both travel in it and are split back out on receive by the reserved `cirreum.`
prefix. No key appears in both bags.

Sharing the wire dictionary is deliberate: a broker-side subscription filter on `cirreum.identifier`
keeps working exactly as before.

A system value is read through `ToString()` rather than a type test, so a value returned in another
representation cannot produce a silent miss. A property written into `ApplicationProperties` under a
reserved key is not forwarded — that space belongs to the framework.

## Application properties are no longer dropped

v1 discarded any application property whose key matched a first-class Service Bus field name
(`MessageId`, `CorrelationId`, `ContentType`, `Subject`, `TimeToLive`, `ReplyTo`), with no exception
and no log. Application properties live in a namespace separate from those fields on the wire, so
the collision the filter avoided did not exist; it removed data to prevent a non-problem.

Those properties now travel. If you set one of those keys and never saw it arrive, it will arrive
now. `StandardProps` is removed.

## Fixed

A batch send skipped common properties entirely for a message that carried none of its own —
`!message.Properties?.ContainsKey(key) == true` evaluates to `false` when the bag is absent, so the
properties were dropped in exactly the case where nothing could conflict with them.

Full steps in [MIGRATION-v2](MIGRATION-v2.md). Requires Cirreum.Messaging 2.0.0.
