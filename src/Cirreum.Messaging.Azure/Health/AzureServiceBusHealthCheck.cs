namespace Cirreum.Messaging.Health;

using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Azure ServiceBus Health Check
/// </summary>
internal class AzureServiceBusHealthCheck(
	IMessagingClient client,
	bool isProduction,
	IMemoryCache memoryCache,
	AzureServiceBusHealthCheckOptions options
) : IServiceProviderHealthCheck<AzureServiceBusHealthCheckOptions>
  , IDisposable {


	private readonly string _cacheKey = $"_azure_servicebus_health_{client.GetType().Name}";
	private readonly TimeSpan _cacheDuration = options.CachedResultTimeout ?? TimeSpan.FromSeconds(60);
	private readonly TimeSpan _failureCacheDuration = TimeSpan.FromSeconds(Math.Max(35, (options.CachedResultTimeout ?? TimeSpan.FromSeconds(60)).TotalSeconds / 2));
	private readonly bool _cacheDisabled = (options.CachedResultTimeout is null || options.CachedResultTimeout.Value.TotalSeconds == 0);
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default) {

		if (this._cacheDisabled) {
			return await this.CheckServiceBusHealthAsync(cancellationToken);
		}

		if (memoryCache.TryGetValue(this._cacheKey, out HealthCheckResult cachedResult)) {
			return cachedResult;
		}

		try {

			await this._semaphore.WaitAsync(cancellationToken);

			// Double-check after acquiring semaphore
			if (memoryCache.TryGetValue(this._cacheKey, out cachedResult)) {
				return cachedResult;
			}

			var result = await this.CheckServiceBusHealthAsync(cancellationToken);

			var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 5));
			var duration = result.Status == HealthStatus.Healthy
				? this._cacheDuration
				: this._failureCacheDuration;

			return memoryCache.Set(this._cacheKey, result, duration + jitter);

		} finally {
			this._semaphore.Release();
		}

	}

	private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(5);

	private async Task<HealthCheckResult> CheckServiceBusHealthAsync(
		CancellationToken cancellationToken = default) {

		var overallStatus = HealthStatus.Healthy;
		var data = new Dictionary<string, object>();
		var exceptions = new List<Exception>();

		try {

			// Process queue health checks
			if (options.Queues?.Length > 0) {
				foreach (var queueOption in options.Queues) {
					queueOption.MessageTtl ??= options.DefaultMessageTtl ?? DefaultTimeToLive;
					await this.CheckQueueHealthAsync(queueOption, data, exceptions, cancellationToken);
				}
			}

			// Process topic health checks
			if (options.Topics?.Length > 0) {
				foreach (var topicOption in options.Topics) {
					topicOption.MessageTtl ??= options.DefaultMessageTtl ?? DefaultTimeToLive;
					await this.CheckTopicHealthAsync(topicOption, data, exceptions, cancellationToken);
				}
			}

			// Process subscription health checks
			if (options.Subscriptions?.Length > 0) {
				foreach (var subscriptionOption in options.Subscriptions) {
					await this.CheckSubscriptionHealthAsync(subscriptionOption, data, exceptions, cancellationToken);
				}
			}

			// Determine overall health status based on exceptions
			if (exceptions.Count > 0) {
				overallStatus = HealthStatus.Unhealthy;
			}

			// Create the health check response
			var description = exceptions.Count > 0
				? "One or more Service Bus checks failed"
				: "All Service Bus checks passed";

			return new HealthCheckResult(
				overallStatus,
				description,
				exceptions.FirstOrDefault(),
				data);

		} catch (Exception ex) {
			return new HealthCheckResult(
				HealthStatus.Unhealthy,
				"Service Bus health check failed unexpectedly",
				ex,
				data);
		}

	}

	private static OutboundMessage CreateMessage(string content, TimeSpan ttl) {

		var props = new Dictionary<string, object> {
			{ "MessageType", typeof(string).AssemblyQualifiedName ?? "System.String" },
			{ "MessageKind", typeof(string).AssemblyQualifiedName ?? "System.String" },
			{ "HealthCheckTimestamp", DateTime.UtcNow.ToString("O") }
		};

		return new OutboundMessage(content, props) {
			Id = $"healthcheck_{Guid.NewGuid()}",
			TimeToLive = ttl
		};

	}

	private async Task CheckQueueHealthAsync(
		AzureServiceBusHealthCheckQueueOptions options,
		Dictionary<string, object> data,
		List<Exception> exceptions,
		CancellationToken cancellationToken) {

		try {

			var queue = client.UseQueue(options.QueueName);

			if (options.ValidateSend) {
				var msg = CreateMessage(options.CheckMessageContent, options.MessageTtl ?? DefaultTimeToLive);
				await queue.PublishMessageAsync(msg, cancellationToken);
				data[$"queue_{options.QueueName}_send"] = "success";
			}

			if (options.ValidateReceive) {
				var message = await queue.PeekMessageAsync(cancellationToken);
				data[$"queue_{options.QueueName}_receive"] = "success";
			}

		} catch (Exception ex) {
			exceptions.Add(ex);
			if (!isProduction) {
				data[$"queue_{options.QueueName}_error"] = ex.Message;
			}
			data[$"queue_{options.QueueName}_error"] = "Failed";
		}
	}

	private async Task CheckTopicHealthAsync(
	   AzureServiceBusHealthCheckTopicOptions options,
	   Dictionary<string, object> data,
	   List<Exception> exceptions,
	   CancellationToken cancellationToken) {
		try {
			if (options.ValidateSend) {
				var msg = CreateMessage(options.CheckMessageContent, options.MessageTtl ?? DefaultTimeToLive);
				await client
					.UseTopic(options.TopicName)
					.BroadcastMessageAsync(msg, cancellationToken);
				data[$"topic_{options.TopicName}_send"] = "success";
			}
		} catch (Exception ex) {
			exceptions.Add(ex);
			if (!isProduction) {
				data[$"topic_{options.TopicName}_error"] = ex.Message;
				return;
			}
			data[$"topic_{options.TopicName}_error"] = "Failed";
		}
	}

	private async Task CheckSubscriptionHealthAsync(
		AzureServiceBusHealthCheckSubscriptionOptions options,
		Dictionary<string, object> data,
		List<Exception> exceptions,
		CancellationToken cancellationToken) {

		var dataSuccessKey = $"subscription_{options.TopicName}_{options.SubscriptionName}_receive";
		var dataErrorKey = $"subscription_{options.TopicName}_{options.SubscriptionName}_error";
		try {
			if (options.ValidateReceive) {
				// Create receiver and try to peek a message
				var message = await client
					.UseSubscription(options.TopicName, options.SubscriptionName)
					.PeekMessageAsync(cancellationToken);
				data[dataSuccessKey] = "Success";
			}
		} catch (Exception ex) {
			exceptions.Add(ex);
			if (!isProduction) {
				data[dataErrorKey] = ex.Message;
				return;
			}
			data[dataErrorKey] = "Failed";
		}

	}

	public void Dispose() {
		this._semaphore?.Dispose();
	}

}