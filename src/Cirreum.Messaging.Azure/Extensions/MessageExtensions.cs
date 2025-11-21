namespace Cirreum.Messaging.Extensions;

internal static class MessageExtensions {

	public static string[] StandardProps = [
		nameof(ServiceBusMessage.MessageId),    // maps to OutboundMessage.Id
		nameof(ServiceBusMessage.CorrelationId),
		nameof(ServiceBusMessage.ContentType),
		nameof(ServiceBusMessage.Subject),
		nameof(ServiceBusMessage.TimeToLive),
		nameof(ServiceBusMessage.ReplyTo)
	];

	private static void SetStringPropertyIfExists(string? directValue, IDictionary<string, object> providerProps, string propertyName, Action<string> setter) {
		if (!string.IsNullOrWhiteSpace(directValue)) {
			setter(directValue);
		} else {
			SetStringPropertyIfExists(propertyName, providerProps, setter);
		}
	}
	private static void SetStringPropertyIfExists(string propertyName, IDictionary<string, object> providerProps, Action<string> setter) {
		if (providerProps.TryGetValue(propertyName, out var value) && value is string stringValue) {
			setter(stringValue);
		}
	}

	public static ServiceBusMessage ToAzureMessage(this OutboundMessage message) {
		var sbm = new ServiceBusMessage(message.Content);

		// Basic message properties
		SetStringPropertyIfExists(message.Id, message.ProviderProperties, nameof(ServiceBusMessage.MessageId),
			value => sbm.MessageId = value);

		SetStringPropertyIfExists(message.ContentType, message.ProviderProperties, nameof(ServiceBusMessage.ContentType),
			value => sbm.ContentType = value);

		SetStringPropertyIfExists(message.CorrelationId, message.ProviderProperties, nameof(ServiceBusMessage.CorrelationId),
			value => sbm.CorrelationId = value);

		SetStringPropertyIfExists(message.ReplyTo, message.ProviderProperties, nameof(ServiceBusMessage.ReplyTo),
			value => sbm.ReplyTo = value);


		// TimeToLive handling
		if (message.TimeToLive.HasValue) {
			sbm.TimeToLive = message.TimeToLive.Value;
		}

		// Subject handling
		SetStringPropertyIfExists(message.Subject, message.ProviderProperties, nameof(ServiceBusMessage.Subject),
			value => sbm.Subject = value);


		// Add application properties
		if (message.Properties != null) {
			foreach (var prop in message.Properties.Where(p =>
				!StandardProps.Contains(p.Key) &&
				p.Value != null)) {
				sbm.ApplicationProperties.Add(prop.Key, prop.Value);
			}
		}

		// Optional properties coming from the message.ProviderProperties
		SetStringPropertyIfExists(nameof(ServiceBusMessage.PartitionKey), message.ProviderProperties,
			value => sbm.PartitionKey = value);

		SetStringPropertyIfExists(nameof(ServiceBusMessage.TransactionPartitionKey), message.ProviderProperties,
			value => sbm.TransactionPartitionKey = value);

		SetStringPropertyIfExists(nameof(ServiceBusMessage.SessionId), message.ProviderProperties,
			value => sbm.SessionId = value);

		SetStringPropertyIfExists(nameof(ServiceBusMessage.ReplyToSessionId), message.ProviderProperties,
			value => sbm.ReplyToSessionId = value);

		SetStringPropertyIfExists(nameof(ServiceBusMessage.To), message.ProviderProperties,
			value => sbm.To = value);

		// Handle scheduled enqueue time if present
		if (message.ProviderProperties.TryGetValue(nameof(ServiceBusMessage.ScheduledEnqueueTime), out var scheduleValue)
			&& scheduleValue is DateTimeOffset scheduleTime) {
			sbm.ScheduledEnqueueTime = scheduleTime;
		}

		return sbm;

	}

	public static IEnumerable<ServiceBusMessage> ToAzureMessages(
		this IEnumerable<OutboundMessage> messages,
		IDictionary<string, object>? commonProperties = null) {

		foreach (var message in messages) {

			// Use existing conversion for standard + message-specific props
			var sbm = message.ToAzureMessage();

			// Add common properties if they don't conflict with message-specific ones
			if (commonProperties != null) {
				foreach (var prop in commonProperties.Where(p =>
					p.Value != null &&
					!message.Properties?.ContainsKey(p.Key) == true)) {
					sbm.ApplicationProperties.Add(prop.Key, prop.Value);
				}
			}

			yield return sbm;

		}

	}

}