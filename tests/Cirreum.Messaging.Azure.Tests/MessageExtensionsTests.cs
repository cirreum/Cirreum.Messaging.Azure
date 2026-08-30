namespace Cirreum.Messaging.Tests;

using Azure.Messaging.ServiceBus;
using Cirreum.Messaging.Extensions;

public class MessageExtensionsTests {

	[Fact]
	public void ToAzureMessage_MapsDirectPropertiesAndContent() {
		var message = new OutboundMessage("hello world") {
			Id = "msg-1",
			ContentType = "text/plain",
			CorrelationId = "corr-1",
			ReplyTo = "replies.v1",
			Subject = "unit.test",
			TimeToLive = TimeSpan.FromMinutes(5)
		};

		var sbm = message.ToAzureMessage();

		sbm.Body.ToString().Should().Be("hello world");
		sbm.MessageId.Should().Be("msg-1");
		sbm.ContentType.Should().Be("text/plain");
		sbm.CorrelationId.Should().Be("corr-1");
		sbm.ReplyTo.Should().Be("replies.v1");
		sbm.Subject.Should().Be("unit.test");
		sbm.TimeToLive.Should().Be(TimeSpan.FromMinutes(5));
	}

	[Fact]
	public void ToAzureMessage_FallsBackToProviderPropertiesWhenDirectValuesAreEmpty() {
		var message = new OutboundMessage("content");
		message.ProviderProperties["MessageId"] = "pp-id";
		message.ProviderProperties["Subject"] = "pp-subject";
		message.ProviderProperties["CorrelationId"] = "pp-corr";

		var sbm = message.ToAzureMessage();

		sbm.MessageId.Should().Be("pp-id");
		sbm.Subject.Should().Be("pp-subject");
		sbm.CorrelationId.Should().Be("pp-corr");
	}

	[Fact]
	public void ToAzureMessage_DirectValueWinsOverProviderProperty() {
		var message = new OutboundMessage("content") {
			Subject = "direct-subject"
		};
		message.ProviderProperties["Subject"] = "pp-subject";

		message.ToAzureMessage().Subject.Should().Be("direct-subject");
	}

	[Fact]
	public void ToAzureMessage_ApplicationPropertiesFlowEvenWhenTheKeyMatchesAServiceBusField() {
		var message = new OutboundMessage("content");
		message.ApplicationProperties["tenant"] = "t1";
		message.ApplicationProperties["Subject"] = "should-not-appear";
		message.ApplicationProperties["MessageId"] = "should-not-appear";

		var sbm = message.ToAzureMessage();

		// Application properties occupy their own namespace on the wire, so a key that happens to
		// match a first-class Service Bus field name is carried rather than dropped.
		sbm.ApplicationProperties.Should().ContainKey("tenant").WhoseValue.Should().Be("t1");
		sbm.ApplicationProperties["Subject"].Should().Be("should-not-appear");
		sbm.ApplicationProperties["MessageId"].Should().Be("should-not-appear");
	}

	[Fact]
	public void ToAzureMessage_MapsAzureSpecificProviderProperties() {
		var scheduled = new DateTimeOffset(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);
		var message = new OutboundMessage("content");
		message.ProviderProperties["PartitionKey"] = "pk-1";
		message.ProviderProperties["To"] = "endpoint-1";
		message.ProviderProperties["ScheduledEnqueueTime"] = scheduled;

		var sbm = message.ToAzureMessage();

		sbm.PartitionKey.Should().Be("pk-1");
		sbm.To.Should().Be("endpoint-1");
		sbm.ScheduledEnqueueTime.Should().Be(scheduled);
	}

	[Fact]
	public void ToAzureMessage_MapsSessionIdFromProviderProperties() {
		var message = new OutboundMessage("content");
		message.ProviderProperties["SessionId"] = "session-1";

		message.ToAzureMessage().SessionId.Should().Be("session-1");
	}

	[Fact]
	public void ToAzureMessages_AddsCommonPropertiesWithoutOverridingMessageSpecificOnes() {
		var message = new OutboundMessage("content");
		message.ApplicationProperties["shared"] = "message-value";

		var common = new Dictionary<string, object> {
			["shared"] = "common-value",
			["extra"] = "common-extra"
		};

		var sbm = new[] { message }.ToAzureMessages(common).Single();

		sbm.ApplicationProperties["shared"].Should().Be("message-value");
		sbm.ApplicationProperties["extra"].Should().Be("common-extra");
	}

	// Property ownership ———————————————————————————————————————

	[Fact]
	public void ToAzureMessage_SystemPropertiesFlowToApplicationProperties() {
		var message = new OutboundMessage("payload")
			.WithSystemProperty("cirreum.node", "node-1");

		var sbm = message.ToAzureMessage();

		sbm.ApplicationProperties["cirreum.node"].Should().Be("node-1");
	}

	[Fact]
	public void ToAzureMessage_DoesNotForwardAReservedKeyFromTheApplicationBag() {
		var message = new OutboundMessage("payload");
		message.ApplicationProperties["cirreum.node"] = "forged";

		var sbm = message.ToAzureMessage();

		sbm.ApplicationProperties.Should().NotContainKey("cirreum.node");
	}

	[Fact]
	public void ToAzureMessage_SystemPropertyWinsOverAForgedApplicationEntry() {
		var message = new OutboundMessage("payload")
			.WithSystemProperty("cirreum.node", "node-1");
		message.ApplicationProperties["cirreum.node"] = "forged";

		var sbm = message.ToAzureMessage();

		sbm.ApplicationProperties["cirreum.node"].Should().Be("node-1");
	}

	[Fact]
	public void ToAzureMessage_DoesNotTransmitANullApplicationValue() {
		var message = new OutboundMessage("payload");
		message.ApplicationProperties["absent"] = null!;

		var sbm = message.ToAzureMessage();

		sbm.ApplicationProperties.Should().NotContainKey("absent");
	}

	[Fact]
	public void ReceivedMessage_SplitsTheWireBagByTheReservedPrefix() {
		var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
			properties: new Dictionary<string, object> {
				["tenant"] = "t1",
				["cirreum.node"] = "node-1",
			});

		var message = new AzureServiceBusQueuePeekedMessage("orders", received);

		message.ApplicationProperties.Should().ContainKey("tenant");
		message.ApplicationProperties.Should().NotContainKey("cirreum.node");
		message.SystemProperties.Should().ContainKey("cirreum.node").WhoseValue.Should().Be("node-1");
		message.SystemProperties.Should().NotContainKey("tenant");
	}

	[Fact]
	public void ReceivedMessage_ReadsASystemValueThatIsNotAString() {
		// A broker whose header model returns something other than a string must not produce a
		// silent miss; the value is read through ToString rather than a type test.
		var received = ServiceBusModelFactory.ServiceBusReceivedMessage(
			properties: new Dictionary<string, object> { ["cirreum.node"] = 42 });

		var message = new AzureServiceBusQueuePeekedMessage("orders", received);

		message.SystemProperties["cirreum.node"].Should().Be("42");
	}
}
