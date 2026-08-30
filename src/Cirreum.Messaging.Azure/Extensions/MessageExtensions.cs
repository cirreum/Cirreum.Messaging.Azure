namespace Cirreum.Messaging.Extensions;

internal static class MessageExtensions {

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


		// Application properties travel as supplied. A key colliding with a first-class Service Bus
		// field is NOT dropped: application properties live in their own namespace on the wire, so
		// there is nothing to collide with. Two entries do not travel — a null value, which has no
		// AMQP mapping worth guessing at, and a key in the reserved system-property space, which is
		// not the application's to write.
		foreach (var prop in message.ApplicationProperties) {
			if (prop.Value is null || SystemProperty.IsReserved(prop.Key)) {
				continue;
			}
			sbm.ApplicationProperties[prop.Key] = prop.Value;
		}

		// System properties share the same wire dictionary. The reserved prefix is what splits them
		// back out on receive, and what keeps them addressable by broker-side subscription filters.
		foreach (var prop in message.SystemProperties) {
			sbm.ApplicationProperties[prop.Key] = prop.Value;
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
		IDictionary<string, object>? commonApplicationProperties = null) {

		foreach (var message in messages) {

			// Use existing conversion for standard + message-specific props
			var sbm = message.ToAzureMessage();

			// Common properties fill in where the message did not set the key itself.
			if (commonApplicationProperties is not null) {
				foreach (var prop in commonApplicationProperties) {
					if (prop.Value is null
						|| SystemProperty.IsReserved(prop.Key)
						|| message.ApplicationProperties.ContainsKey(prop.Key)) {
						continue;
					}
					sbm.ApplicationProperties[prop.Key] = prop.Value;
				}
			}

			yield return sbm;

		}

	}

}