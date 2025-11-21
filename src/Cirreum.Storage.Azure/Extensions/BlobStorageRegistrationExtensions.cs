namespace Cirreum.Storage.Extensions;

using Cirreum.ServiceProvider.Configuration;
using Cirreum.Storage;
using Cirreum.Storage.Configuration;
using Cirreum.Storage.Health;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

internal static class BlobStorageRegistrationExtensions {

	public static void AddAzureCloudStorageClient(
			this IServiceCollection services,
			string serviceKey,
			AzureBlobStorageInstanceSettings settings) {

		// Register Keyed Service Factory
		services.AddKeyedSingleton<ICloudStorageClient>(
			serviceKey,
			(sp, key) => settings.CreateAzureBlobStorageClient());

		// Register Default (non-Keyed) Service Factory (wraps the keyed registration)
		if (serviceKey.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase)) {
			services.TryAddSingleton(sp => sp.GetRequiredKeyedService<ICloudStorageClient>(serviceKey));
		}

	}

	private static AzureBlobStorageClient CreateAzureBlobStorageClient(
		this AzureBlobStorageInstanceSettings settings) {

		return new AzureBlobStorageClient(
			!string.IsNullOrEmpty(settings.ConnectionString)
			? new BlobServiceClient(settings.ConnectionString, settings.ClientOptions)
			: new BlobServiceClient(settings.ServiceUri, new DefaultAzureCredential(), settings.ClientOptions)
		);

	}

	public static AzureBlobStorageHealthCheck CreateAzureBlobStorageHealthCheck(
		this IServiceProvider serviceProvider,
		string serviceKey,
		AzureBlobStorageInstanceSettings settings) {
		var env = serviceProvider.GetRequiredService<IHostEnvironment>();
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();
		var client = serviceProvider.GetRequiredKeyedService<ICloudStorageClient>(serviceKey);
		return new AzureBlobStorageHealthCheck(client, env.IsProduction(), cache, settings.HealthOptions ?? new());
	}
}