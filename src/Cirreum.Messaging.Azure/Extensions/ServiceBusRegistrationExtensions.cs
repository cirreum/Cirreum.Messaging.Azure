namespace Cirreum.Messaging.Extensions;

using Cirreum.Messaging.Configuration;
using Cirreum.Messaging.Health;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Convenient extension methods for registering Azure ServiceBus as the implementation
/// for the <see cref="ProviderType.Messaging"/> services.
/// </summary>
internal static class ServiceBusRegistrationExtensions {

	public static void AddAzureMessagingClient(
		this IServiceCollection services,
		string serviceKey,
		AzureServiceBusInstanceSettings settings) {

		// Register Keyed Service Factory
		services.AddKeyedSingleton<IMessagingClient>(
			serviceKey,
			(sp, key) => sp.CreateAzureServiceBusClient(settings));

		// Register Default (non-Keyed) Service Factory (wraps the keyed registration)
		if (serviceKey.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase)) {
			services.TryAddSingleton(sp => sp.GetRequiredKeyedService<IMessagingClient>(serviceKey));
		}

	}

	private static AzureServiceBusClient CreateAzureServiceBusClient(
		this IServiceProvider serviceProvider,
		AzureServiceBusInstanceSettings settings) {

		var connectionString = settings.ConnectionString ?? "";
		var useCredentials = !connectionString.Contains("endpoint=", StringComparison.OrdinalIgnoreCase);
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();

		return new AzureServiceBusClient(
			useCredentials
			? new ServiceBusClient(settings.ConnectionString, new DefaultAzureCredential(), settings.ClientOptions)
			: new ServiceBusClient(settings.ConnectionString, settings.ClientOptions),
			cache);

	}

	public static AzureServiceBusHealthCheck CreateAzureServiceBusHealthCheck(
		this IServiceProvider serviceProvider,
		string serviceKey,
		AzureServiceBusInstanceSettings settings) {
		var env = serviceProvider.GetRequiredService<IHostEnvironment>();
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();
		var client = serviceProvider.GetRequiredKeyedService<IMessagingClient>(serviceKey);
		return new AzureServiceBusHealthCheck(client, env.IsProduction(), cache, settings.HealthOptions ?? new());
	}

}