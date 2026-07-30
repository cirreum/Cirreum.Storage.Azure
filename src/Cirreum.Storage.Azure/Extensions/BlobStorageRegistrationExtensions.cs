namespace Cirreum.Storage.Extensions;

using Cirreum.Providers.Configuration;
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

		// Mirrors the client construction below: a non-empty connection string is key-based
		// authentication, which a Credential block cannot apply to.
		if (!string.IsNullOrEmpty(settings.ConnectionString) && settings.Credential is not null) {
			throw new InvalidOperationException(
				"A Credential block is configured but the connection value is a key-based connection string. " +
				"Identity-based authentication requires the blob service URI as the connection value.");
		}

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
			: new BlobServiceClient(settings.ServiceUri, settings.GetCredential(), settings.ClientOptions)
		);

	}

	private static TokenCredential GetCredential(
		this AzureBlobStorageInstanceSettings settings) {

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
				$"CredentialMode '{credential.Mode}' is not supported by the Azure Blob Storage provider."),

		};

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