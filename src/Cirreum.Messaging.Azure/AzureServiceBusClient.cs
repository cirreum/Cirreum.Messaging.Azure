namespace Cirreum.Messaging;

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

internal sealed class AzureServiceBusClient(
	ServiceBusClient client,
	IMemoryCache cache,
	int cacheTimeout = 30)
	: IMessagingClient
	, IAsyncDisposable {

	// Cache Control
	private const string Queue_Sender_Prefix = "sender_queue_";
	private const string Queue_Receiver_Prefix = "receiver_queue_";
	private const string Topic_Sender_Prefix = "sender_topic_";
	private const string Subscription_Receiver_Prefix = "receiver_subscription_";
	private readonly TimeSpan _senderReceiverTimeout = TimeSpan.FromMinutes(cacheTimeout);
	private readonly ConcurrentDictionary<string, byte> _cacheKeys = [];

	// Receiver tuning is creation-time-only, and receivers are cached by name — so the
	// tuning recorded at a name's first request is that name's identity for the client's
	// lifetime (deliberately surviving cache eviction), and later requests must agree
	// rather than silently receiving an instance tuned differently than they asked.
	private readonly ConcurrentDictionary<string, ReceiverTuning?> _receiverTunings = [];

	private void EnsureConsistentTuning(string cacheKey, string display, ReceiverTuning? requested) {
		var normalized = requested is { IsEmpty: true } ? null : requested;
		var recorded = _receiverTunings.GetOrAdd(cacheKey, normalized);
		if (!Equals(recorded, normalized)) {
			throw new InvalidOperationException(
				$"Receiver '{display}' was already requested with different tuning " +
				$"(first: {Describe(recorded)}; now: {Describe(normalized)}). Receivers are " +
				"cached by name and tuning applies at creation only, so every request for " +
				"the same receiver must carry the same tuning.");
		}
	}

	private static string Describe(ReceiverTuning? tuning) =>
		tuning is null ? "none" : $"PrefetchCount={tuning.PrefetchCount?.ToString() ?? "broker default"}";
	private T GetOrCreateCachedClient<T>(string cacheKey, Func<T> factory) where T : IAsyncDisposable {
		_cacheKeys.TryAdd(cacheKey, 0);
		return cache.GetOrCreate(cacheKey, entry => {
			entry.SlidingExpiration = _senderReceiverTimeout;
			entry.RegisterPostEvictionCallback(async (key, value, reason, state) => {
				if (value is T disposable) {
					await disposable.DisposeAsync();
				}
			});
			return factory();
		})!;
	}

	// Cleanup!
	/// <inheritdoc/>
	public async ValueTask DisposeAsync() {
		foreach (var key in _cacheKeys.Keys) {
			if (cache.TryGetValue(key, out var value) && value is IAsyncDisposable disposer) {
				cache.Remove(key);  // Explicitly remove from cache
				await disposer.DisposeAsync();
			}
		}
	}

	// Direct Access
	/// <inheritdoc/>
	public Task UseClient<T>(Func<T, Task> handler) {
		if (client is not T tclient) {
			throw new InvalidOperationException($"T Type {typeof(T).Name}' is unsupported.");
		}
		return handler(tclient);
	}


	// Sender/Receiver Factories
	/// <inheritdoc/>
	public IMessagingQueue UseQueue(string queue) {
		ArgumentException.ThrowIfNullOrEmpty(queue);
		return new AzureServiceBusQueue(
			this.UseQueueSender(queue),
			this.UseQueueReceiver(queue));
	}
	/// <inheritdoc/>
	public IMessagingQueueSender UseQueueSender(string queue) {
		ArgumentException.ThrowIfNullOrEmpty(queue);
		var sender = this.GetOrCreateCachedClient(
			Queue_Sender_Prefix + queue,
			() => client.CreateSender(queue));
		return new AzureServiceBusQueueSender(queue, sender);
	}
	/// <inheritdoc/>
	public IMessagingQueueReceiver UseQueueReceiver(string queue) =>
		this.UseQueueReceiverCore(queue, tuning: null);

	/// <inheritdoc/>
	public IMessagingQueueReceiver UseQueueReceiver(string queue, ReceiverTuning tuning) {
		ArgumentNullException.ThrowIfNull(tuning);
		return this.UseQueueReceiverCore(queue, tuning);
	}

	private IMessagingQueueReceiver UseQueueReceiverCore(string queue, ReceiverTuning? tuning) {
		ArgumentException.ThrowIfNullOrEmpty(queue);
		var cacheKey = Queue_Receiver_Prefix + queue;
		this.EnsureConsistentTuning(cacheKey, $"queue:{queue}", tuning);
		var receiver = this.GetOrCreateCachedClient(
			cacheKey,
			() => CreateReceiverOptions(tuning) is { } options
				? client.CreateReceiver(queue, options)
				: client.CreateReceiver(queue));
		return new AzureServiceBusQueueReceiver(queue, receiver);
	}

	private static ServiceBusReceiverOptions? CreateReceiverOptions(ReceiverTuning? tuning) =>
		tuning?.PrefetchCount is { } prefetch
			? new ServiceBusReceiverOptions { PrefetchCount = prefetch }
			: null;
	/// <inheritdoc/>
	public IMessagingTopicSender UseTopic(string topic) {
		ArgumentException.ThrowIfNullOrEmpty(topic);
		var sender = this.GetOrCreateCachedClient(
			Topic_Sender_Prefix + topic,
			() => client.CreateSender(topic));
		return new AzureServiceBusTopicSender(topic, sender);
	}
	/// <inheritdoc/>
	public IMessagingSubscriptionReceiver UseSubscription(string topic, string subscription) =>
		this.UseSubscriptionCore(topic, subscription, tuning: null);

	/// <inheritdoc/>
	public IMessagingSubscriptionReceiver UseSubscription(string topic, string subscription, ReceiverTuning tuning) {
		ArgumentNullException.ThrowIfNull(tuning);
		return this.UseSubscriptionCore(topic, subscription, tuning);
	}

	private IMessagingSubscriptionReceiver UseSubscriptionCore(string topic, string subscription, ReceiverTuning? tuning) {
		ArgumentException.ThrowIfNullOrEmpty(topic);
		ArgumentException.ThrowIfNullOrEmpty(subscription);
		var cacheKey = $"{Subscription_Receiver_Prefix}{topic}_{subscription}";
		this.EnsureConsistentTuning(cacheKey, $"subscription:{topic}/{subscription}", tuning);
		var receiver = this.GetOrCreateCachedClient(
			cacheKey,
			() => CreateReceiverOptions(tuning) is { } options
				? client.CreateReceiver(topic, subscription, options)
				: client.CreateReceiver(topic, subscription));
		return new AzureServiceBusSubscriptionReceiver(topic, subscription, receiver);
	}

}