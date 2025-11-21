namespace Cirreum.Messaging;

internal abstract class AzureServiceBusCompletableMessage(
	ServiceBusReceiver receiver,
	ServiceBusReceivedMessage originalMessage)
	: AzureServiceBusReceivedMessage(originalMessage) {

	public Task CompleteMessageAsync(CancellationToken cancellationToken = default) {
		if (message is null) {
			throw new InvalidOperationException($"The underlying {nameof(ServiceBusMessage)} is null and cannot be completed.");
		}
		return receiver.CompleteMessageAsync(message, cancellationToken);
	}
	public Task AbandonMessageAsync(CancellationToken cancellationToken = default) {
		if (message is null) {
			throw new InvalidOperationException($"The underlying {nameof(ServiceBusMessage)} is null and cannot be abandoned.");
		}
		return receiver.AbandonMessageAsync(message, null, cancellationToken);
	}
	public Task DeferMessageAsync(CancellationToken cancellationToken = default) {
		if (message is null) {
			throw new InvalidOperationException($"The underlying {nameof(ServiceBusMessage)} is null and cannot be deferred.");
		}
		return receiver.DeferMessageAsync(message, null, cancellationToken);
	}
	public Task RenewLockAsync(CancellationToken cancellationToken = default) {
		if (message is null) {
			throw new InvalidOperationException($"The underlying {nameof(ServiceBusMessage)} is null and cannot be renewed.");
		}
		return receiver.RenewMessageLockAsync(message, cancellationToken);
	}
	public Task DeadLetterMessageAsync(
		string reason,
		string description,
		CancellationToken cancellationToken = default) {
		if (message is null) {
			throw new InvalidOperationException($"The underlying {nameof(ServiceBusMessage)} is null and cannot be dead lettered.");
		}
		return receiver.DeadLetterMessageAsync(
			message,
			reason,
			description,
			cancellationToken);
	}

}