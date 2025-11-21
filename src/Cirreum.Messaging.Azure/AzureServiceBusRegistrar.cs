namespace Cirreum.Messaging;

using Cirreum.Messaging.Configuration;
using Cirreum.Messaging.Extensions;
using Cirreum.Messaging.Health;
using Cirreum.ServiceProvider;
using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

/// <summary>
/// Registrar responsible for auto-registering any configured messaging clients for the
/// 'Azure' Service Providers in the Messaging section of application settings.
/// </summary>
public sealed class AzureServiceBusRegistrar() :
	ServiceProviderRegistrar<
		AzureServiceBusSettings,
		AzureServiceBusInstanceSettings,
		AzureServiceBusHealthCheckOptions> {

	/// <inheritdoc/>
	public override ProviderType ProviderType => ProviderType.Messaging;

	/// <inheritdoc/>
	public override string ProviderName => "Azure";

	/// <inheritdoc/>
	public override string[] ActivitySourceNames { get; } = [$"{typeof(ServiceBusClient).Namespace}.*"];

	/// <inheritdoc/>
	protected override void AddServiceProviderInstance(
		IServiceCollection services,
		string serviceKey,
		AzureServiceBusInstanceSettings settings) {
		services.AddAzureMessagingClient(serviceKey, settings);
	}

	/// <inheritdoc/>
	protected override IServiceProviderHealthCheck<AzureServiceBusHealthCheckOptions> CreateHealthCheck(
		IServiceProvider serviceProvider,
		string serviceKey,
		AzureServiceBusInstanceSettings settings) {
		return serviceProvider.CreateAzureServiceBusHealthCheck(serviceKey, settings);
	}

}