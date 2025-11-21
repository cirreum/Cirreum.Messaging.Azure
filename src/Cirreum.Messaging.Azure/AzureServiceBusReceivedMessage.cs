namespace Cirreum.Messaging;

using System.Collections.Immutable;
using System.Text;

internal abstract class AzureServiceBusReceivedMessage(
	ServiceBusReceivedMessage originalMessage)
	: IMessagingReceivableMessage {

	protected readonly ServiceBusReceivedMessage message = originalMessage;

	private static readonly IReadOnlyDictionary<string, object> EmptyProperties = ImmutableDictionary<string, object>.Empty;
	private IReadOnlyDictionary<string, object>? _providerProperties;
	private static readonly byte[] EmptyBytes = [];
	private byte[]? _contentBytes;
	private string? _contentStringCache;


	// base message

	public string Id => this.message.MessageId ?? string.Empty;
	public byte[] Content => this._contentBytes ??= this.message.Body?.ToArray() ?? EmptyBytes;
	public string ContentString => this._contentStringCache ??= Encoding.UTF8.GetString(this.Content);
	public string ContentType => this.message.ContentType ?? string.Empty;
	public string CorrelationId => this.message.CorrelationId ?? string.Empty;
	public string ReplyTo => this.message.ReplyTo ?? string.Empty;


	// receivable message

	public DateTimeOffset EnqueuedTime => this.message.EnqueuedTime.UtcDateTime;
	public DateTimeOffset ExpiresAt => this.message.ExpiresAt;
	public int DeliveryCount => this.message.DeliveryCount;
	public IReadOnlyDictionary<string, object> Properties => this.message.ApplicationProperties ?? EmptyProperties;
	public IReadOnlyDictionary<string, object> ProviderProperties => this._providerProperties ??= this.InitializeProviderProperties();
	private IReadOnlyDictionary<string, object> InitializeProviderProperties() {
		if (this.message is null) {
			return EmptyProperties;
		}
		return new Dictionary<string, object> {
			[nameof(ServiceBusReceivedMessage.PartitionKey)] = this.message.PartitionKey ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.TransactionPartitionKey)] = this.message.TransactionPartitionKey ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.SessionId)] = this.message.SessionId ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.ReplyToSessionId)] = this.message.ReplyToSessionId ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.Subject)] = this.message.Subject ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.To)] = this.message.To ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.LockToken)] = this.message.LockToken ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.LockedUntil)] = this.message.LockedUntil,
			[nameof(ServiceBusReceivedMessage.TimeToLive)] = this.message.TimeToLive,
			[nameof(ServiceBusReceivedMessage.DeadLetterReason)] = this.message.DeadLetterReason ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.DeadLetterErrorDescription)] = this.message.DeadLetterErrorDescription ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.DeadLetterSource)] = this.message.DeadLetterSource ?? string.Empty,
			[nameof(ServiceBusReceivedMessage.EnqueuedSequenceNumber)] = this.message.EnqueuedSequenceNumber,
			[nameof(ServiceBusReceivedMessage.SequenceNumber)] = this.message.SequenceNumber,
			[nameof(ServiceBusReceivedMessage.ScheduledEnqueueTime)] = this.message.ScheduledEnqueueTime,
			[nameof(ServiceBusReceivedMessage.State)] = this.message.State,
			["State.String"] = $"{this.message.State}"
		};
	}

}