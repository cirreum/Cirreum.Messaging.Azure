namespace Cirreum.Messaging;

using Cirreum.Messaging.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal sealed class AzureServiceBusTopicSender(
	string topic,
	ServiceBusSender sender)
	: IMessagingTopicSender {

	public string Topic => topic;

	public Task BroadcastMessageAsync(OutboundMessage message, CancellationToken cancellationToken = default) {
		return sender.SendMessageAsync(message.ToAzureMessage(), cancellationToken);
	}
	public Task BroadcastMessagesAsync(IEnumerable<OutboundMessage> messages, IDictionary<string, object>? commonProperties = null, CancellationToken cancellationToken = default) {
		return sender.SendMessagesAsync(messages.ToAzureMessages(commonProperties), cancellationToken);
	}

}