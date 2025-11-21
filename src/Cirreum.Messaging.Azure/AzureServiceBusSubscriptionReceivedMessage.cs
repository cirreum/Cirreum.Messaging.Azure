namespace Cirreum.Messaging;

internal sealed class AzureServiceBusSubscriptionReceivedMessage(
	string topic,
	string subscription,
	ServiceBusReceiver receiver,
	ServiceBusReceivedMessage originalMessage)
	: AzureServiceBusCompletableMessage(receiver, originalMessage)
	, IMessagingSubscriptionReceivedMessage {
	public string Topic => topic;
	public string Subscription => subscription;
}