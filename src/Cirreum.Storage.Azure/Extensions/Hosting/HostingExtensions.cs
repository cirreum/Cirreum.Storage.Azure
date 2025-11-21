namespace Microsoft.Extensions.Hosting;

using Cirreum.Storage.Configuration;
using Cirreum.Storage.Health;
using Microsoft.Extensions.DependencyInjection;

public static class HostingExtensions {

	/// <summary>
	/// Adds a manually configured <see cref="ICloudStorageClient"/> instance for Azure BlobStorage.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="settings">The configured instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddAzureBlobStorageClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		AzureBlobStorageInstanceSettings settings,
		Action<BlobClientOptions>? configureClientOptions = null,
		Action<AzureBlobStorageHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		// Configure client options
		settings.ClientOptions ??= new BlobClientOptions();
		configureClientOptions?.Invoke(settings.ClientOptions);

		// Configure health options
		settings.HealthOptions ??= new AzureBlobStorageHealthCheckOptions();
		configureHealthCheckOptions?.Invoke(settings.HealthOptions);

		// Reuse our Registrar...
		var registrar = new AzureBlobStorageRegistrar();
		registrar.RegisterInstance(
			serviceKey,
			settings,
			builder.Services,
			builder.Configuration);

		return builder;

	}

	/// <summary>
	/// Adds a manually configured <see cref="ICloudStorageClient"/> instance for Azure BlobStorage.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="configure">The callback to configure the instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddAzureBlobStorageClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		Action<AzureBlobStorageInstanceSettings> configure,
		Action<BlobClientOptions>? configureClientOptions = null,
		Action<AzureBlobStorageHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureBlobStorageInstanceSettings();
		configure?.Invoke(settings);
		if (string.IsNullOrWhiteSpace(settings.Name)) {
			settings.Name = serviceKey;
		}

		return AddAzureBlobStorageClient(builder, serviceKey, settings, configureClientOptions, configureHealthCheckOptions);

	}

	/// <summary>
	/// Adds a manually configured <see cref="ICloudStorageClient"/> instance for Azure BlobStorage.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="connectionString">The user provided connection string.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddAzureBlobStorageClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		string connectionString,
		Action<BlobClientOptions>? configureClientOptions = null,
		Action<AzureBlobStorageHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureBlobStorageInstanceSettings() {
			ConnectionString = connectionString,
			Name = serviceKey
		};

		return AddAzureBlobStorageClient(builder, serviceKey, settings, configureClientOptions, configureHealthCheckOptions);

	}

}