namespace Cirreum.Messaging.Tests;

using Cirreum.Messaging.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>
/// The registrar keeps process-wide static registries of instance keys and
/// connection-string hashes, so every test uses unique values for both.
/// </summary>
public class AddAzureMessagingClientCompositionTests {

	private static string UniqueKey() {
		return $"sb-{Guid.NewGuid():N}";
	}

	private static string UniqueConnectionString() {
		return $"Endpoint=sb://unit-{Guid.NewGuid():N}.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=dGVzdEtleQ==";
	}

	private static HostApplicationBuilder CreateBuilder() {
		var builder = Host.CreateEmptyApplicationBuilder(new());
		builder.Services.AddMemoryCache();
		return builder;
	}

	[Fact]
	public async Task ConnectionStringOverload_RegistersAKeyedMessagingClient() {
		var key = UniqueKey();
		var builder = CreateBuilder();

		builder.AddAzureMessagingClient(key, UniqueConnectionString());

		await using var provider = builder.Services.BuildServiceProvider();
		provider.GetRequiredKeyedService<IMessagingClient>(key)
			.Should().BeOfType<AzureServiceBusClient>();
	}

	[Fact]
	public async Task SettingsCallback_ResolvesConnectionFromTheConnectionStringsSection() {
		var key = UniqueKey();
		var builder = CreateBuilder();
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
			[$"ConnectionStrings:{key}"] = UniqueConnectionString()
		});

		// Name defaults to the service key, which drives ConnectionStrings resolution
		builder.AddAzureMessagingClient(key, settings => { });

		await using var provider = builder.Services.BuildServiceProvider();
		provider.GetRequiredKeyedService<IMessagingClient>(key)
			.Should().BeOfType<AzureServiceBusClient>();
	}

	[Fact]
	public void MissingConnectionString_FailsAtRegistrationTime() {
		var builder = CreateBuilder();

		var act = () => builder.AddAzureMessagingClient(UniqueKey(), settings => { });

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*ConnectionString*");
	}

	[Fact]
	public void DuplicateServiceKey_FailsAtRegistrationTime() {
		var key = UniqueKey();
		var builder = CreateBuilder();
		builder.AddAzureMessagingClient(key, UniqueConnectionString());

		var act = () => builder.AddAzureMessagingClient(key, UniqueConnectionString());

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*already been registered*");
	}

	[Fact]
	public void DuplicateConnectionString_FailsAtRegistrationTime() {
		var connectionString = UniqueConnectionString();
		var builder = CreateBuilder();
		builder.AddAzureMessagingClient(UniqueKey(), connectionString);

		var act = () => builder.AddAzureMessagingClient(UniqueKey(), connectionString);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*connection*");
	}

	[Fact]
	public async Task DefaultKey_AlsoRegistersTheUnkeyedMessagingClient() {
		// "default" is a process-wide singleton key: this must be the only test using it
		var builder = CreateBuilder();

		builder.AddAzureMessagingClient("default", UniqueConnectionString());

		await using var provider = builder.Services.BuildServiceProvider();
		provider.GetRequiredService<IMessagingClient>()
			.Should().BeSameAs(provider.GetRequiredKeyedService<IMessagingClient>("default"));
	}

	[Fact]
	public async Task HealthChecksEnabled_RegistersAWorkingHealthCheck() {
		var key = UniqueKey();
		var builder = CreateBuilder();

		builder.AddAzureMessagingClient(key, settings => {
			settings.ConnectionString = UniqueConnectionString();
			settings.HealthChecks = true;
		});

		await using var provider = builder.Services.BuildServiceProvider();
		var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>()
			.Value.Registrations;

		var registration = registrations.Should()
			.ContainSingle(r => r.Name.Contains(key, StringComparison.OrdinalIgnoreCase))
			.Subject;
		registration.Factory(provider).Should().BeOfType<AzureServiceBusHealthCheck>();
	}

	[Fact]
	public async Task HealthChecksDisabled_RegistersNoHealthCheck() {
		var key = UniqueKey();
		var builder = CreateBuilder();

		builder.AddAzureMessagingClient(key, UniqueConnectionString());

		await using var provider = builder.Services.BuildServiceProvider();
		var options = provider.GetService<IOptions<HealthCheckServiceOptions>>();
		var registered = options?.Value.Registrations
			.Any(r => r.Name.Contains(key, StringComparison.OrdinalIgnoreCase)) ?? false;

		registered.Should().BeFalse();
	}

}
