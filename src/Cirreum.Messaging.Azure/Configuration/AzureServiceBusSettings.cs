namespace Cirreum.Messaging.Configuration;

using Cirreum.Messaging.Health;
using Cirreum.ServiceProvider.Configuration;

public class AzureServiceBusSettings
	: ServiceProviderSettings<
		AzureServiceBusInstanceSettings,
		AzureServiceBusHealthCheckOptions>;