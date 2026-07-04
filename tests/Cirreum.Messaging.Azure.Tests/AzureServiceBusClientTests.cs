namespace Cirreum.Messaging.Tests;

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Caching.Memory;

public class AzureServiceBusClientTests {

	private const string TestConnectionString =
		"Endpoint=sb://unit-tests.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdEtleQ==";

	private static (AzureServiceBusClient Client, ServiceBusClient Inner, MemoryCache Cache) CreateClient() {
		var inner = new ServiceBusClient(TestConnectionString);
		var cache = new MemoryCache(new MemoryCacheOptions());
		return (new AzureServiceBusClient(inner, cache), inner, cache);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void Factories_ThrowOnMissingEntityNames(string? name) {
		var (client, _, _) = CreateClient();

		((Action)(() => client.UseQueue(name!))).Should().Throw<ArgumentException>();
		((Action)(() => client.UseQueueSender(name!))).Should().Throw<ArgumentException>();
		((Action)(() => client.UseQueueReceiver(name!))).Should().Throw<ArgumentException>();
		((Action)(() => client.UseTopic(name!))).Should().Throw<ArgumentException>();
		((Action)(() => client.UseSubscription(name!, "sub"))).Should().Throw<ArgumentException>();
		((Action)(() => client.UseSubscription("topic", name!))).Should().Throw<ArgumentException>();
	}

	[Fact]
	public void UseQueueSender_CachesTheUnderlyingSenderPerQueue() {
		var (client, _, cache) = CreateClient();

		client.UseQueueSender("orders.v1");
		client.UseQueueSender("orders.v1");

		cache.Count.Should().Be(1);

		client.UseQueueSender("invoices.v1");

		cache.Count.Should().Be(2);
	}

	[Fact]
	public void SendersAndReceivers_UseDistinctCacheEntriesPerRole() {
		var (client, _, cache) = CreateClient();

		client.UseQueueSender("orders.v1");
		client.UseQueueReceiver("orders.v1");
		client.UseTopic("notices.v1");
		client.UseSubscription("notices.v1", "api-head");

		cache.Count.Should().Be(4);
	}

	[Fact]
	public async Task UseClient_HandsTheUnderlyingServiceBusClientToTheHandler() {
		var (client, inner, _) = CreateClient();

		ServiceBusClient? observed = null;
		await client.UseClient<ServiceBusClient>(c => {
			observed = c;
			return Task.CompletedTask;
		});

		observed.Should().BeSameAs(inner);
	}

	[Fact]
	public async Task UseClient_ThrowsForAnUnsupportedClientType() {
		var (client, _, _) = CreateClient();

		var act = () => client.UseClient<string>(_ => Task.CompletedTask);

		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task DisposeAsync_EvictsAllCachedSendersAndReceivers() {
		var (client, _, cache) = CreateClient();
		client.UseQueueSender("orders.v1");
		client.UseTopic("notices.v1");
		cache.Count.Should().Be(2);

		await client.DisposeAsync();

		cache.Count.Should().Be(0);
	}

}
