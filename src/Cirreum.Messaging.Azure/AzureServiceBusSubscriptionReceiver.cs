namespace Cirreum.Messaging;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

internal sealed class AzureServiceBusSubscriptionReceiver(
	string topic,
	string subscription,
	ServiceBusReceiver receiver)
	: IMessagingSubscriptionReceiver {

	public string Topic => topic;

	public string Subscription => subscription;

	// Peek

	public async Task<IMessagingSubscriptionPeekedMessage> PeekMessageAsync(CancellationToken cancellationToken = default) {
		var msg = await receiver.PeekMessageAsync(cancellationToken: cancellationToken);
		return new AzureServiceBusSubscriptionPeekedMessage(topic, subscription, msg);
	}
	public async Task<IReadOnlyList<IMessagingSubscriptionPeekedMessage>> PeekMessagesAsync(int maxMessages, CancellationToken cancellationToken = default) {
		var msgs = await receiver.PeekMessagesAsync(maxMessages, cancellationToken: cancellationToken);
		var rmsgs = new List<IMessagingSubscriptionPeekedMessage>();
		foreach (var msg in msgs) {
			rmsgs.Add(new AzureServiceBusSubscriptionPeekedMessage(topic, subscription, msg));
		}
		return rmsgs;
	}


	// Receive

	public async Task<IMessagingSubscriptionReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default) {
		var msg = await receiver.ReceiveMessageAsync(maxWaitTime, cancellationToken);
		return new AzureServiceBusSubscriptionReceivedMessage(topic, subscription, receiver, msg);
	}
	public async IAsyncEnumerable<IMessagingSubscriptionReceivedMessage> ReceiveMessagesStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) {
		await foreach (var msg in receiver.ReceiveMessagesAsync(cancellationToken)) {
			yield return new AzureServiceBusSubscriptionReceivedMessage(topic, subscription, receiver, msg);
		}
	}
	public async Task<IReadOnlyList<IMessagingSubscriptionReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default) {
		var msgs = await receiver.ReceiveMessagesAsync(maxMessages, maxWaitTime, cancellationToken);
		var result = new List<IMessagingSubscriptionReceivedMessage>(msgs.Count);
		foreach (var msg in msgs) {
			result.Add(new AzureServiceBusSubscriptionReceivedMessage(topic, subscription, receiver, msg));
		}
		return result;
	}

}