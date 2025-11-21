namespace Cirreum.Messaging;

internal sealed class AzureServiceBusSubscriptionPeekedMessage(
	string topic,
	string subscription,
	ServiceBusReceivedMessage message)
	: AzureServiceBusReceivedMessage(message)
	, IMessagingSubscriptionPeekedMessage {
	public string Topic => topic;
	public string Subscription => subscription;
}