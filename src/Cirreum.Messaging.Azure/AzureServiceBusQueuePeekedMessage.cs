namespace Cirreum.Messaging;

internal class AzureServiceBusQueuePeekedMessage(
	string queue,
	ServiceBusReceivedMessage message)
	: AzureServiceBusReceivedMessage(message)
	, IMessagingQueuePeekedMessage {
	public string Queue => queue;
}