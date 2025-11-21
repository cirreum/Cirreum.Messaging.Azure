namespace Cirreum.Messaging.Health;

public class AzureServiceBusHealthCheckSubscriptionOptions {

	public string TopicName { get; set; } = "";

	public string SubscriptionName { get; set; } = "";

	public bool ValidateReceive { get; set; } = true;

	public bool ValidateManagement { get; set; } = false;

}