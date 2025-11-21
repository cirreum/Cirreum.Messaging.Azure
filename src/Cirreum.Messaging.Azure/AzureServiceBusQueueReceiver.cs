namespace Cirreum.Messaging;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

internal sealed class AzureServiceBusQueueReceiver(
	string queue,
	ServiceBusReceiver receiver)
	: IMessagingQueueReceiver {

	public string Queue => queue;

	// Peek

	public async Task<IMessagingQueuePeekedMessage> PeekMessageAsync(CancellationToken cancellationToken = default) {
		var msg = await receiver.PeekMessageAsync(cancellationToken: cancellationToken);
		return new AzureServiceBusQueuePeekedMessage(queue, msg);
	}
	public async Task<IReadOnlyList<IMessagingQueuePeekedMessage>> PeekMessagesAsync(int maxMessages, CancellationToken cancellationToken = default) {
		var msgs = await receiver.PeekMessagesAsync(maxMessages, cancellationToken: cancellationToken);
		var rmsgs = new List<IMessagingQueuePeekedMessage>();
		foreach (var msg in msgs) {
			rmsgs.Add(new AzureServiceBusQueuePeekedMessage(queue, msg));
		}
		return rmsgs;
	}


	// Receive

	public async Task<IMessagingQueueReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default) {
		var msg = await receiver.ReceiveMessageAsync(maxWaitTime, cancellationToken);
		return new AzureServiceBusQueueReceivedMessage(queue, receiver, msg);
	}
	public async IAsyncEnumerable<IMessagingQueueReceivedMessage> ReceiveMessagesStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) {
		await foreach (var msg in receiver.ReceiveMessagesAsync(cancellationToken)) {
			yield return new AzureServiceBusQueueReceivedMessage(queue, receiver, msg);
		}
	}
	public async Task<IReadOnlyList<IMessagingQueueReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default) {
		var msgs = await receiver.ReceiveMessagesAsync(maxMessages, maxWaitTime, cancellationToken);
		var result = new List<IMessagingQueueReceivedMessage>(msgs.Count);
		foreach (var msg in msgs) {
			result.Add(new AzureServiceBusQueueReceivedMessage(queue, receiver, msg));
		}
		return result;
	}

}