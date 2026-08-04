namespace Cirreum.Messaging.Tests;

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Caching.Memory;

public class AzureServiceBusClientTuningTests {

	private const string TestConnectionString =
		"Endpoint=sb://unit-tests.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdEtleQ==";

	private static AzureServiceBusClient CreateClient() {
		var inner = new ServiceBusClient(TestConnectionString);
		var cache = new MemoryCache(new MemoryCacheOptions());
		return new AzureServiceBusClient(inner, cache);
	}

	[Fact]
	public void TunedReceiver_AppliesPrefetchToTheUnderlyingReceiver() {
		var client = CreateClient();

		var receiver = client.UseQueueReceiver("orders.v1", new ReceiverTuning { PrefetchCount = 50 });

		((AzureServiceBusQueueReceiver)receiver).Receiver.PrefetchCount.Should().Be(50);
	}

	[Fact]
	public void TunedSubscription_AppliesPrefetchToTheUnderlyingReceiver() {
		var client = CreateClient();

		var receiver = client.UseSubscription("notices.v1", "api-head", new ReceiverTuning { PrefetchCount = 25 });

		((AzureServiceBusSubscriptionReceiver)receiver).Receiver.PrefetchCount.Should().Be(25);
	}

	[Fact]
	public void SameReceiver_SameTuning_IsReturnedFromCacheWithoutConflict() {
		var client = CreateClient();
		var tuning = new ReceiverTuning { PrefetchCount = 50 };

		var act = () => {
			client.UseQueueReceiver("orders.v1", tuning);
			client.UseQueueReceiver("orders.v1", new ReceiverTuning { PrefetchCount = 50 });
		};

		act.Should().NotThrow();
	}

	[Fact]
	public void SameReceiver_DifferentTuning_Throws() {
		var client = CreateClient();
		client.UseQueueReceiver("orders.v1", new ReceiverTuning { PrefetchCount = 50 });

		var act = () => client.UseQueueReceiver("orders.v1", new ReceiverTuning { PrefetchCount = 10 });

		act.Should().Throw<InvalidOperationException>().WithMessage("*different tuning*");
	}

	[Fact]
	public void UntunedThenTuned_Throws() {
		var client = CreateClient();
		client.UseQueueReceiver("orders.v1");

		var act = () => client.UseQueueReceiver("orders.v1", new ReceiverTuning { PrefetchCount = 50 });

		act.Should().Throw<InvalidOperationException>().WithMessage("*different tuning*");
	}

	[Fact]
	public void EmptyTuning_IsEquivalentToUntuned() {
		var client = CreateClient();
		client.UseQueueReceiver("orders.v1");

		var act = () => client.UseQueueReceiver("orders.v1", new ReceiverTuning());

		act.Should().NotThrow();
	}

}
