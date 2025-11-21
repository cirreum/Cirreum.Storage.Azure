namespace Cirreum.Storage.Configuration;

using Cirreum.ServiceProvider.Configuration;
using Cirreum.Storage.Health;

public class AzureBlobStorageSettings
	: ServiceProviderSettings<
		AzureBlobStorageInstanceSettings,
		AzureBlobStorageHealthCheckOptions>;