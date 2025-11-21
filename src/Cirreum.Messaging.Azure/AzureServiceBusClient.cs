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
	public IMessagingQueueReceiver UseQueueReceiver(string queue) {
		ArgumentException.ThrowIfNullOrEmpty(queue);
		var receiver = this.GetOrCreateCachedClient(
			Queue_Receiver_Prefix + queue,
			() => client.CreateReceiver(queue));
		return new AzureServiceBusQueueReceiver(queue, receiver);
	}
	/// <inheritdoc/>
	public IMessagingTopicSender UseTopic(string topic) {
		ArgumentException.ThrowIfNullOrEmpty(topic);
		var sender = this.GetOrCreateCachedClient(
			Topic_Sender_Prefix + topic,
			() => client.CreateSender(topic));
		return new AzureServiceBusTopicSender(topic, sender);
	}
	/// <inheritdoc/>
	public IMessagingSubscriptionReceiver UseSubscription(string topic, string subscription) {
		ArgumentException.ThrowIfNullOrEmpty(topic);
		ArgumentException.ThrowIfNullOrEmpty(subscription);
		var receiver = this.GetOrCreateCachedClient(
			$"{Subscription_Receiver_Prefix}{topic}_{subscription}",
			() => client.CreateReceiver(topic, subscription));
		return new AzureServiceBusSubscriptionReceiver(topic, subscription, receiver);
	}

}