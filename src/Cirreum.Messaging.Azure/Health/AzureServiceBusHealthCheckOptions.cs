namespace Cirreum.Messaging.Health;

using Cirreum.Health;

/// <summary>
/// Represents a collection of settings that configure a
/// <see cref="AzureServiceBusHealthCheck"> health check</see>.
/// </summary>
public class AzureServiceBusHealthCheckOptions
	: ServiceProviderHealthCheckOptions {

	/// <summary>
	/// The default Ttl (TimeToLive) when sending health check messages.
	/// </summary>
	public TimeSpan? DefaultMessageTtl { get; set; }

	/// <summary>
	/// The optional one or more queues to monitor.
	/// </summary>
	public AzureServiceBusHealthCheckQueueOptions[] Queues { get; set; } = [];

	/// <summary>
	/// The optional one or more topics to monitor.
	/// </summary>
	public AzureServiceBusHealthCheckTopicOptions[] Topics { get; set; } = [];

	/// <summary>
	/// The optional one or more topic subscriptions to monitor.
	/// </summary>
	public AzureServiceBusHealthCheckSubscriptionOptions[] Subscriptions { get; set; } = [];

}