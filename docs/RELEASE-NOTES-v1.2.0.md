# Cirreum.Messaging.Azure 1.2.0 — prefetch reaches Service Bus

## Why this release exists

The middle rung of the receiver-tuning chain. `Cirreum.Messaging` 1.1.1 grew the contract
(`ReceiverTuning` + tuned `UseQueueReceiver` / `UseSubscription` overloads); this release maps
it onto the broker. Before it, `PrefetchCount` configured anywhere in a Cirreum app reached
nothing — the SDK default silently applied.

## What's new

**The tuned overloads are implemented.** `PrefetchCount` maps onto
`ServiceBusReceiverOptions.PrefetchCount` at receiver creation:

```csharp
var receiver = client.UseQueueReceiver("orders.pending.v1", new ReceiverTuning {
	PrefetchCount = 50
});
```

**Tuning is part of a receiver's identity.** Receivers are cached by name, so the tuning
recorded at a name's first request holds for the client's lifetime (deliberately surviving
cache eviction): a later request for the same receiver with *different* tuning throws instead
of silently returning an instance tuned differently than asked. An empty `ReceiverTuning` is
equivalent to no tuning at all.

## Coordinated downstream work

`Cirreum.Runtime.Messaging` completes the chain in its own release: it passes the configured
`ReceiverOptions.PrefetchCount` through these overloads, and honors
`ReceiverOptions.MaxAutoLockRenewalDuration` in its receive loop over the existing
`RenewLockAsync` — lock renewal is a processing-time behavior, not a receiver-creation option.

## Compatibility

Additive. Untuned calls behave exactly as before. The only new runtime behavior is deliberate:
conflicting tuning for an already-created receiver throws instead of being silently ignored.

## See also

- `docs/CHANGELOG.md` — the enumerated changes
- `Cirreum.Messaging` 1.1.0 / 1.1.1 release notes — the chain head and its same-day correction
