namespace Cirreum.Messaging.Extensions;

using Cirreum.Messaging.Configuration;
using Cirreum.Messaging.Health;
using Cirreum.Providers.Configuration;
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

		// Mirrors the client construction below: an "endpoint=" connection string is key-based
		// authentication, which a Credential block cannot apply to.
		if ((settings.ConnectionString ?? "").Contains("endpoint=", StringComparison.OrdinalIgnoreCase) &&
			settings.Credential is not null) {
			throw new InvalidOperationException(
				"A Credential block is configured but the connection value is a key-based connection string. " +
				"Identity-based authentication requires the fully qualified namespace as the connection value.");
		}

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
			? new ServiceBusClient(settings.ConnectionString, settings.GetCredential(), settings.ClientOptions)
			: new ServiceBusClient(settings.ConnectionString, settings.ClientOptions),
			cache);

	}

	private static TokenCredential GetCredential(
		this AzureServiceBusInstanceSettings settings) {

		var tenantId = string.IsNullOrWhiteSpace(settings.Identifier) ? null : settings.Identifier;
		var credential = settings.Credential ?? new CredentialSettings();
		var identityId = string.IsNullOrWhiteSpace(credential.IdentityId) ? null : credential.IdentityId;

		return credential.Mode switch {

			CredentialMode.Default => new DefaultAzureCredential(new DefaultAzureCredentialOptions {
				TenantId = tenantId,
				ManagedIdentityClientId = identityId,
			}),

			CredentialMode.ManagedIdentity => new ManagedIdentityCredential(
				identityId is null
					? ManagedIdentityId.SystemAssigned
					: ManagedIdentityId.FromUserAssignedClientId(identityId)),

			CredentialMode.Developer => new ChainedTokenCredential(
				new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId }),
				new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
				new AzurePowerShellCredential(new AzurePowerShellCredentialOptions { TenantId = tenantId })),

			_ => throw new InvalidOperationException(
				$"CredentialMode '{credential.Mode}' is not supported by the Azure Service Bus provider."),

		};

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