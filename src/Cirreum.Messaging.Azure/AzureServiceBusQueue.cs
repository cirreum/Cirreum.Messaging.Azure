namespace Cirreum.Messaging;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal sealed class AzureServiceBusQueue(
	IMessagingQueueSender sender,
	IMessagingQueueReceiver receiver)
	: IMessagingQueue {

	public string Queue => sender.Queue;


	// Publish Messages

	public Task PublishMessageAsync(OutboundMessage message, CancellationToken cancellationToken = default) {
		return sender.PublishMessageAsync(message, cancellationToken);
	}
	public Task PublishMessagesAsync(IEnumerable<OutboundMessage> messages, IDictionary<string, object>? commonProperties = null, CancellationToken cancellationToken = default) {
		return sender.PublishMessagesAsync(messages, commonProperties, cancellationToken);
	}


	// Peek Messages

	public Task<IMessagingQueuePeekedMessage> PeekMessageAsync(CancellationToken cancellationToken = default) {
		return receiver.PeekMessageAsync(cancellationToken);
	}
	public Task<IReadOnlyList<IMessagingQueuePeekedMessage>> PeekMessagesAsync(int maxMessages, CancellationToken cancellationToken = default) {
		return receiver.PeekMessagesAsync(maxMessages, cancellationToken);
	}


	// Receive Messages

	public Task<IMessagingQueueReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default) {
		return receiver.ReceiveMessageAsync(maxWaitTime, cancellationToken);
	}
	public Task<IReadOnlyList<IMessagingQueueReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default) {
		return receiver.ReceiveMessagesAsync(maxMessages, maxWaitTime, cancellationToken);
	}
	public IAsyncEnumerable<IMessagingQueueReceivedMessage> ReceiveMessagesStreamAsync(CancellationToken cancellationToken = default) {
		return receiver.ReceiveMessagesStreamAsync(cancellationToken);
	}

}