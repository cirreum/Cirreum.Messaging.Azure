namespace Cirreum.Messaging.Health;

public class AzureServiceBusHealthCheckTopicOptions {

	public string TopicName { get; set; } = "";

	/// <summary>
	/// The Ttl when sending health check messages.
	/// </summary>
	public TimeSpan? MessageTtl { get; set; }

	public bool ValidateSend { get; set; } = true;

	public bool ValidateManagement { get; set; } = false;

	public string CheckMessageContent { get; set; } = "HealthCheck";

}