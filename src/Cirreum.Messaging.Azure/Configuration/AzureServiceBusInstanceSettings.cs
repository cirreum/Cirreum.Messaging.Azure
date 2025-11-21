namespace Cirreum.Messaging.Configuration;

using Cirreum.Messaging.Health;
using Cirreum.ServiceProvider.Configuration;

public class AzureServiceBusInstanceSettings
	: ServiceProviderInstanceSettings<AzureServiceBusHealthCheckOptions> {

	/// <summary>
	/// Overrides the base health check options with Azure-specific settings.
	/// </summary>
	public override AzureServiceBusHealthCheckOptions? HealthOptions { get; set; }
		= new AzureServiceBusHealthCheckOptions();

	/// <summary>
	/// Optional <see cref="ServiceBusClientOptions"/>.
	/// </summary>
	public ServiceBusClientOptions? ClientOptions { get; set; }

}