namespace Cirreum.Storage.Health;

using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;

/// <summary>
/// Azure Blob Storage health check.
/// </summary>
/// <remarks>
/// Creates new instance of Azure Blob Storage health check.
/// </remarks>
/// <param name="client">
/// The <see cref="BlobServiceClient"/> used to perform Azure Blob Storage operations.
/// Azure SDK recommends treating clients as singletons <see href="https://devblogs.microsoft.com/azure-sdk/lifetime-management-and-thread-safety-guarantees-of-azure-sdk-net-clients/"/>,
/// so this should be the exact same instance used by other parts of the application.
/// </param>
/// <param name="isProduction"><see langword="true"/> when running in production; otherwise <see langword="false"/>.</param>
/// <param name="memoryCache"></param>
/// <param name="options">Optional settings used by the health check.</param>
public sealed class AzureBlobStorageHealthCheck(
	ICloudStorageClient client,
	bool isProduction,
	IMemoryCache memoryCache,
	AzureBlobStorageHealthCheckOptions options
) : IServiceProviderHealthCheck<AzureBlobStorageHealthCheckOptions>
  , IDisposable {

	private readonly string _cacheKey = $"_storage_health_{client.AccountName.ToLowerInvariant()}";
	private readonly TimeSpan _cacheDuration = options.CachedResultTimeout ?? TimeSpan.FromSeconds(60);
	private readonly TimeSpan _failureCacheDuration = TimeSpan.FromSeconds(Math.Max(35, (options.CachedResultTimeout ?? TimeSpan.FromSeconds(60)).TotalSeconds / 2));
	private readonly bool _cacheDisabled = (options.CachedResultTimeout is null || options.CachedResultTimeout.Value.TotalSeconds == 0);
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	/// <inheritdoc />
	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {

		if (_cacheDisabled) {
			return await this.CheckStorageHealthAsync(context, cancellationToken);
		}

		if (memoryCache.TryGetValue(_cacheKey, out HealthCheckResult cachedResult)) {
			return cachedResult;
		}

		try {

			await _semaphore.WaitAsync(cancellationToken);

			// Double-check after acquiring semaphore
			if (memoryCache.TryGetValue(_cacheKey, out cachedResult)) {
				return cachedResult;
			}

			var result = await this.CheckStorageHealthAsync(context, cancellationToken);

			var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 5));
			var duration = result.Status == HealthStatus.Healthy
				? _cacheDuration
				: _failureCacheDuration;

			return memoryCache.Set(_cacheKey, result, duration + jitter);

		} finally {
			_semaphore.Release();
		}

	}

	private async Task<HealthCheckResult> CheckStorageHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {

		return await client.UsingClientAsync<BlobServiceClient, HealthCheckResult>(async bsc => {

			try {

				//
				// Note:
				//
				// BlobServiceClient.GetPropertiesAsync() cannot be used with only the role assignment
				// "Storage Blob Data Contributor," so BlobServiceClient.GetBlobContainersAsync() is used instead
				// to probe service health.
				//
				// However, BlobContainerClient.GetPropertiesAsync() does have sufficient permissions.
				//

				await bsc
					.GetBlobContainersAsync(cancellationToken: cancellationToken)
					.AsPages(pageSizeHint: 1)
					.GetAsyncEnumerator(cancellationToken)
					.MoveNextAsync()
					.ConfigureAwait(false);

				if (string.IsNullOrEmpty(options.ContainerName)) {
					if (isProduction) {
						return HealthCheckResult.Healthy($"Connected to storage service");
					}
					return HealthCheckResult.Healthy($"Connected to azure storage service: {client.AccountName}");
				}

				var containerClient = bsc.GetBlobContainerClient(options.ContainerName);
				await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

				if (isProduction) {
					return HealthCheckResult.Healthy($"Connected to storage service and container.");
				}
				return HealthCheckResult.Healthy(
					$"Connected to azure storage service: {client.AccountName} with Container: {options.ContainerName}");

			} catch (Exception ex) {
				return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
			}

		});

	}

	public void Dispose() {
		this._semaphore?.Dispose();
	}

}