namespace Cirreum.Storage.Configuration;

using Cirreum.ServiceProvider.Configuration;
using Cirreum.Storage.Health;

public class AzureBlobStorageInstanceSettings
	: ServiceProviderInstanceSettings<AzureBlobStorageHealthCheckOptions> {

	/// <summary>
	/// Overrides the base health check options with Azure-specific settings.
	/// </summary>
	public override AzureBlobStorageHealthCheckOptions? HealthOptions { get; set; }
		= new AzureBlobStorageHealthCheckOptions();

	/// <summary>
	/// Optional <see cref="BlobClientOptions"/>.
	/// </summary>
	public BlobClientOptions? ClientOptions { get; set; }

	/// <summary>
	/// A <see cref="Uri"/> referencing the blob service.
	/// This is likely to be similar to "https://{account_name}.blob.core.windows.net".
	/// </summary>
	/// <remarks>
	/// Must not contain shared access signature.
	/// </remarks>
	internal Uri? ServiceUri { get; private set; }

	/// <inheritdoc/>
	public override void ParseConnectionString(string rawValue) {
		if (Uri.TryCreate(rawValue, UriKind.Absolute, out var uri)) {
			this.ServiceUri = uri;
			return;
		}
		this.ConnectionString = rawValue;
	}

}