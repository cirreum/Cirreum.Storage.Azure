namespace Cirreum.Storage;

using Cirreum.ServiceProvider;
using Cirreum.ServiceProvider.Health;
using Cirreum.Storage.Configuration;
using Cirreum.Storage.Extensions;
using Cirreum.Storage.Health;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registrar responsible for auto-registering any configured storage clients for the
/// 'Azure' Service Providers in the Storage section of application settings.
/// </summary>
public sealed class AzureBlobStorageRegistrar :
	ServiceProviderRegistrar<
		AzureBlobStorageSettings,
		AzureBlobStorageInstanceSettings,
		AzureBlobStorageHealthCheckOptions> {

	/// <inheritdoc/>
	public override ProviderType ProviderType { get; } = ProviderType.Storage;

	/// <inheritdoc/>
	public override string ProviderName => "Azure";

	/// <inheritdoc/>
	public override string[] ActivitySourceNames { get; } = [$"{typeof(BlobServiceClient).Namespace}.*"];

	/// <inheritdoc/>
	protected override void AddServiceProviderInstance(
		IServiceCollection services,
		string serviceKey,
		AzureBlobStorageInstanceSettings settings) {
		services.AddAzureCloudStorageClient(serviceKey, settings);
	}

	/// <inheritdoc/>
	protected override IServiceProviderHealthCheck<AzureBlobStorageHealthCheckOptions> CreateHealthCheck(
		IServiceProvider serviceProvider,
		string serviceKey,
		AzureBlobStorageInstanceSettings settings) {
		return serviceProvider.CreateAzureBlobStorageHealthCheck(serviceKey, settings);
	}

}