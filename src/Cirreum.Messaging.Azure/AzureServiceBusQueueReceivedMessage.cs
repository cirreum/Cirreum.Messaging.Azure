namespace Cirreum.Messaging;

internal sealed class AzureServiceBusQueueReceivedMessage(
	string queue,
	ServiceBusReceiver receiver,
	ServiceBusReceivedMessage originalMessage)
	: AzureServiceBusCompletableMessage(receiver, originalMessage)
	, IMessagingQueueReceivedMessage {
	public string Queue => queue;
}