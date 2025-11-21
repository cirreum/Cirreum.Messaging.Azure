namespace Cirreum.Messaging;

using Cirreum.Messaging.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal sealed class AzureServiceBusQueueSender(
	string queue,
	ServiceBusSender sender)
	: IMessagingQueueSender {

	public string Queue => queue;

	public Task PublishMessageAsync(OutboundMessage message, CancellationToken cancellationToken = default) {
		return sender.SendMessageAsync(message.ToAzureMessage(), cancellationToken);
	}
	public Task PublishMessagesAsync(IEnumerable<OutboundMessage> messages, IDictionary<string, object>? commonProperties = null, CancellationToken cancellationToken = default) {
		return sender.SendMessagesAsync(messages.ToAzureMessages(commonProperties), cancellationToken);
	}

}