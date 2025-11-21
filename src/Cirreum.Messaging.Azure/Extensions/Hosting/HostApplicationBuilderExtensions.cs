namespace Microsoft.Extensions.Hosting;

using Cirreum.Messaging.Configuration;
using Cirreum.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

public static class HostApplicationBuilderExtensions {

	/// <summary>
	/// Adds a manually configured <see cref="IMessagingClient"/> instance for Azure ServiceBus.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="settings">The configured instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddAzureMessagingClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		AzureServiceBusInstanceSettings settings,
		Action<ServiceBusClientOptions>? configureClientOptions = null,
		Action<AzureServiceBusHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		// Configure client options
		settings.ClientOptions ??= new ServiceBusClientOptions();
		configureClientOptions?.Invoke(settings.ClientOptions);

		// Configure health options
		settings.HealthOptions ??= new AzureServiceBusHealthCheckOptions();
		configureHealthCheckOptions?.Invoke(settings.HealthOptions);

		// Reuse our Registrar...
		var registrar = new AzureServiceBusRegistrar();
		registrar.RegisterInstance(
			serviceKey,
			settings,
			builder.Services,
			builder.Configuration);

		return builder;

	}

	/// <summary>
	/// Adds a manually configured <see cref="IMessagingClient"/> instance for Azure ServiceBus.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="configure">The callback to configure the instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddAzureMessagingClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		Action<AzureServiceBusInstanceSettings> configure,
		Action<ServiceBusClientOptions>? configureClientOptions = null,
		Action<AzureServiceBusHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureServiceBusInstanceSettings();
		configure?.Invoke(settings);
		if (string.IsNullOrWhiteSpace(settings.Name)) {
			settings.Name = serviceKey;
		}

		return AddAzureMessagingClient(builder, serviceKey, settings, configureClientOptions, configureHealthCheckOptions);

	}

	/// <summary>
	/// Adds a manually configured <see cref="IMessagingClient"/> instance for Azure ServiceBus.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="connectionString">The callback to configure the instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddAzureMessagingClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		string connectionString,
		Action<ServiceBusClientOptions>? configureClientOptions = null,
		Action<AzureServiceBusHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureServiceBusInstanceSettings() {
			ConnectionString = connectionString,
			Name = serviceKey
		};

		return AddAzureMessagingClient(builder, serviceKey, settings, configureClientOptions, configureHealthCheckOptions);

	}


}