namespace Cirreum.Messaging.Tests;

using Cirreum.Messaging.Health;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class AzureServiceBusHealthCheckTests {

	private static readonly HealthCheckContext Context = new();

	private static IMessagingClient CreateHealthyClient() {
		var client = Substitute.For<IMessagingClient>();
		client.UseQueue(Arg.Any<string>()).Returns(Substitute.For<IMessagingQueue>());
		client.UseTopic(Arg.Any<string>()).Returns(Substitute.For<IMessagingTopicSender>());
		client.UseSubscription(Arg.Any<string>(), Arg.Any<string>())
			.Returns(Substitute.For<IMessagingSubscriptionReceiver>());
		return client;
	}

	private static AzureServiceBusHealthCheck CreateHealthCheck(
		IMessagingClient client,
		AzureServiceBusHealthCheckOptions options,
		bool isProduction = false) {
		return new AzureServiceBusHealthCheck(
			client,
			isProduction,
			new MemoryCache(new MemoryCacheOptions()),
			options);
	}

	[Fact]
	public async Task AllChecksPass_ReportsHealthyWithPerEntityData() {
		var client = CreateHealthyClient();
		var options = new AzureServiceBusHealthCheckOptions {
			CachedResultTimeout = TimeSpan.Zero,
			Queues = [new() { QueueName = "orders.v1" }],
			Topics = [new() { TopicName = "notices.v1" }],
			Subscriptions = [new() { TopicName = "notices.v1", SubscriptionName = "api-head" }]
		};

		var result = await CreateHealthCheck(client, options).CheckHealthAsync(Context);

		result.Status.Should().Be(HealthStatus.Healthy);
		result.Data.Should().ContainKeys(
			"queue_orders.v1_send",
			"queue_orders.v1_receive",
			"topic_notices.v1_send",
			"subscription_notices.v1_api-head_receive");
	}

	[Fact]
	public async Task QueueFailure_ReportsUnhealthyWithExceptionDetailOutsideProduction() {
		var client = CreateHealthyClient();
		var failingQueue = Substitute.For<IMessagingQueue>();
		failingQueue.PublishMessageAsync(Arg.Any<OutboundMessage>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException(new InvalidOperationException("Send failed")));
		client.UseQueue("orders.v1").Returns(failingQueue);

		var options = new AzureServiceBusHealthCheckOptions {
			CachedResultTimeout = TimeSpan.Zero,
			Queues = [new() { QueueName = "orders.v1" }]
		};

		var result = await CreateHealthCheck(client, options).CheckHealthAsync(Context);

		result.Status.Should().Be(HealthStatus.Unhealthy);
		result.Exception.Should().BeOfType<InvalidOperationException>();
		result.Data["queue_orders.v1_error"].Should().Be("Send failed");
	}

	[Fact]
	public async Task QueueFailure_MasksExceptionDetailInProduction() {
		var client = CreateHealthyClient();
		var failingQueue = Substitute.For<IMessagingQueue>();
		failingQueue.PublishMessageAsync(Arg.Any<OutboundMessage>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException(new InvalidOperationException("Send failed")));
		client.UseQueue("orders.v1").Returns(failingQueue);

		var options = new AzureServiceBusHealthCheckOptions {
			CachedResultTimeout = TimeSpan.Zero,
			Queues = [new() { QueueName = "orders.v1" }]
		};

		var result = await CreateHealthCheck(client, options, isProduction: true).CheckHealthAsync(Context);

		result.Status.Should().Be(HealthStatus.Unhealthy);
		result.Data["queue_orders.v1_error"].Should().Be("Failed");
	}

	[Fact]
	public async Task CachedResult_SecondCallDoesNotReprobeTheBroker() {
		var client = CreateHealthyClient();
		var options = new AzureServiceBusHealthCheckOptions {
			CachedResultTimeout = TimeSpan.FromSeconds(60),
			Queues = [new() { QueueName = "orders.v1" }]
		};
		var healthCheck = CreateHealthCheck(client, options);

		await healthCheck.CheckHealthAsync(Context);
		await healthCheck.CheckHealthAsync(Context);

		client.Received(1).UseQueue("orders.v1");
	}

	[Fact]
	public async Task DisabledCache_ReprobesOnEveryCall() {
		var client = CreateHealthyClient();
		var options = new AzureServiceBusHealthCheckOptions {
			CachedResultTimeout = TimeSpan.Zero,
			Queues = [new() { QueueName = "orders.v1" }]
		};
		var healthCheck = CreateHealthCheck(client, options);

		await healthCheck.CheckHealthAsync(Context);
		await healthCheck.CheckHealthAsync(Context);

		client.Received(2).UseQueue("orders.v1");
	}

}
