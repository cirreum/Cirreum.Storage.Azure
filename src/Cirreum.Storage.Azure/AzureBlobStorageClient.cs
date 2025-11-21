namespace Cirreum.Storage;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Cirreum.Storage.Extensions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Initializes a new instance of the <see cref="AzureBlobStorageClient"/> class.
/// </summary>
/// <param name="client">The BLOB service client.</param>
internal sealed class AzureBlobStorageClient(
	BlobServiceClient client)
	: ICloudStorageClient {

	/// <inheritdoc/>
	public string AccountName { get; } = client.AccountName;

	/// <inheritdoc/>
	public Task UsingClientAsync<TClient>(Func<TClient, Task> callback) where TClient : class {
		if (client is not TClient tclient) {
			throw new InvalidOperationException($"TClient type {typeof(TClient).Name}' is unsupported.");
		}
		return callback(tclient);
	}

	/// <inheritdoc/>
	public Task<TResult> UsingClientAsync<TClient, TResult>(Func<TClient, Task<TResult>> callback) where TClient : class {
		if (client is not TClient tclient) {
			throw new InvalidOperationException($"TClient type {typeof(TClient).Name}' is unsupported.");
		}
		return callback(tclient);
	}

	/// <inheritdoc/>
	public async Task CreateIfNotExistsAsync(string containerId) {
		var containerClient = client.GetBlobContainerClient(containerId);
		await containerClient.CreateIfNotExistsAsync();
	}

	/// <inheritdoc/>
	public async Task DeleteContainerAsync(string containerId) {
		await client.DeleteBlobContainerAsync(containerId);
	}

	/// <inheritdoc/>
	public async Task DeleteBlobsAsync(
		string containerId,
		string prefix,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: token)) {
			await containerClient.DeleteBlobIfExistsAsync(blobItem.Name, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: token);
		}
	}

	/// <inheritdoc/>
	public async Task DeleteBlobAsync(
		string containerId,
		string blobId,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		await containerClient.DeleteBlobIfExistsAsync(blobId, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: token);
	}


	/// <inheritdoc/>
	public async Task DownloadFileAsync(
		string containerId,
		string blobId,
		string destinationFilePath,
		DownloadBlobOptions? options = null,
		CancellationToken token = default) {

		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);

		if (options is null) {
			await blobClient.DownloadToAsync(destinationFilePath, cancellationToken: token);
			return;
		}

		BlobRequestConditions? conditions = null;
		if (options.RequestConditions is not null) {
			conditions = options.RequestConditions.AsAzureBlobRequestConditions();
		}
		BlobDownloadToOptions toOptions = new() {
			Conditions = conditions,
			ProgressHandler = options.ProgressHandler,
			TransferOptions = options.TransferOptions.AsAzureTransferOptions(),
			TransferValidation = options.TransferValidation.AsAzureDownloadTransferValidation()
		};

		await blobClient.DownloadToAsync(destinationFilePath, toOptions, cancellationToken: token);

	}


	/// <inheritdoc/>
	public async Task<IDictionary<string, string>> GetMetaDataAsync(
		string containerId,
		string blobId,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		BlobProperties response = await blobClient.GetPropertiesAsync(cancellationToken: token);
		return response.Metadata ?? new Dictionary<string, string>();
	}

	/// <inheritdoc/>
	public async Task<string?> SetMetaDataAsync(
		string containerId,
		string blobId,
		IDictionary<string, string> metadata,
		RequestBlobConditions? conditions,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		BlobRequestConditions? blobRequestConditions = null;
		if (conditions is not null) {
			blobRequestConditions = conditions.AsAzureBlobRequestConditions();
		}
		BlobInfo response = await blobClient.SetMetadataAsync(metadata, blobRequestConditions, cancellationToken: token);
		return response.VersionId;
	}

	/// <inheritdoc/>
	public async Task SetTagsAsync(
		string containerId,
		string blobId,
		IDictionary<string, string> tags,
		RequestBlobConditions? conditions,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		BlobRequestConditions? blobRequestConditions = null;
		if (conditions is not null) {
			blobRequestConditions = conditions.AsAzureBlobRequestConditions();
		}
		await blobClient.SetTagsAsync(tags, blobRequestConditions, cancellationToken: token);
	}

	/// <inheritdoc/>
	public async Task<LeaseInfo> AcquireLeaseAsync(
		string containerId,
		string blobId,
		TimeSpan duration,
		BlobConditions? conditions = null,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		var leaseClient = blobClient.GetBlobLeaseClient();
		RequestConditions? requestCondition = null;
		if (conditions is not null) {
			requestCondition = conditions.AsAzureRequestConditions();
		}
		var leaseResponse = await leaseClient.AcquireAsync(duration, requestCondition, token);
		return new(
			leaseResponse.Value.LeaseId,
			leaseResponse.Value.LeaseTime,
			leaseResponse.Value.LastModified,
			leaseResponse.Value.ETag.ToString("G"));
	}

	/// <inheritdoc/>
	public async Task<LeaseInfo> RenewLeaseAsync(
		string containerId,
		string blobId,
		BlobConditions? conditions = null,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		var leaseClient = blobClient.GetBlobLeaseClient();
		RequestConditions? requestCondition = null;
		if (conditions is not null) {
			requestCondition = conditions.AsAzureRequestConditions();
		}
		var leaseResponse = await leaseClient.RenewAsync(requestCondition, token);
		return new(
			leaseResponse.Value.LeaseId,
			leaseResponse.Value.LeaseTime,
			leaseResponse.Value.LastModified,
			leaseResponse.Value.ETag.ToString("G"));
	}

	/// <inheritdoc/>
	public async Task<LeaseInfo> ReleaseLeaseAsync(
		string containerId,
		string blobId,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		var leaseClient = blobClient.GetBlobLeaseClient();
		var leaseResponse = await leaseClient.ReleaseAsync(cancellationToken: token);
		return new(
			leaseClient.LeaseId,
			null,
			leaseResponse.Value.LastModified,
			leaseResponse.Value.ETag.ToString("G"));
	}

	/// <inheritdoc/>
	public async Task<LeaseInfo> BreakLeaseAsync(
		string containerId,
		string blobId,
		TimeSpan? breakPeriod,
		BlobConditions? conditions = null,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		var leaseClient = blobClient.GetBlobLeaseClient();
		RequestConditions? requestCondition = null;
		if (conditions is not null) {
			requestCondition = conditions.AsAzureRequestConditions();
		}
		var leaseResponse = await leaseClient.BreakAsync(breakPeriod, requestCondition, token);
		return new(
			leaseResponse.Value.LeaseId,
			leaseResponse.Value.LeaseTime,
			leaseResponse.Value.LastModified,
			leaseResponse.Value.ETag.ToString("G"));
	}


	/// <inheritdoc/>
	public async Task<string?> UploadFileAsync(
		string containerId,
		string blobId,
		string sourceFilePath,
		bool overwrite,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		BlobContentInfo response = await blobClient.UploadAsync(sourceFilePath, overwrite, token);
		return response.VersionId;
	}
	/// <inheritdoc/>
	public async Task<string?> UploadFileAsync(
		string containerId,
		string blobId,
		string sourceFilePath,
		bool overwrite,
		IDictionary<string, string> metaData,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		if (overwrite is false) {
			if (metaData is null || metaData.Count == 0) {
				BlobContentInfo r1 = await blobClient.UploadAsync(sourceFilePath, false, token);
				return r1.VersionId;
			}
			await blobClient.UploadAsync(sourceFilePath, false, token);
			BlobInfo metaResponse = await blobClient.SetMetadataAsync(metaData, cancellationToken: token);
			return metaResponse.VersionId;
		}
		if (metaData is null || metaData.Count == 0) {
			BlobContentInfo r3 = await blobClient.UploadAsync(sourceFilePath, true, token);
			return r3.VersionId;
		}
		var options = new BlobUploadOptions {
			Metadata = metaData
		};
		BlobContentInfo r4 = await blobClient.UploadAsync(sourceFilePath, options, token);
		return r4.VersionId;
	}
	/// <inheritdoc/>
	public async Task<string?> UploadFileAsync(
		string containerId,
		string blobId,
		string sourceFilePath,
		UploadBlobOptions options,
		CancellationToken token = default) {

		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);

		BlobRequestConditions? conditions = null;
		if (options.RequestConditions is not null) {
			conditions = options.RequestConditions.AsAzureBlobRequestConditions();
		}

		BlobUploadOptions uploadOptions = new() {
			Conditions = conditions,
			Metadata = options.Metadata,
			Tags = options.Tags,
			ProgressHandler = options.ProgressHandler,
			TransferOptions = options.TransferOptions.AsAzureTransferOptions(),
			TransferValidation = options.TransferValidation.AsAzureUploadTransferValidation()
		};

		var response = await blobClient.UploadAsync(sourceFilePath, uploadOptions, token);
		return response.Value.VersionId;

	}

	/// <inheritdoc/>
	public async Task<string?> UploadContentAsync(
		string containerId,
		string blobId,
		string content,
		bool overwrite,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		BlobContentInfo response = await blobClient.UploadAsync(new BinaryData(content), overwrite, token);
		return response.VersionId;
	}
	/// <inheritdoc/>
	public async Task<string?> UploadContentAsync(
		string containerId,
		string blobId,
		string content,
		UploadBlobOptions options,
		CancellationToken token = default) {

		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);

		BlobRequestConditions? conditions = null;
		if (options.RequestConditions is not null) {
			conditions = options.RequestConditions.AsAzureBlobRequestConditions();
		}

		BlobUploadOptions uploadOptions = new() {
			Conditions = conditions,
			Metadata = options.Metadata,
			Tags = options.Tags,
			ProgressHandler = options.ProgressHandler,
			TransferOptions = options.TransferOptions.AsAzureTransferOptions(),
			TransferValidation = options.TransferValidation.AsAzureUploadTransferValidation()
		};

		var response = await blobClient.UploadAsync(new BinaryData(content), uploadOptions, token);
		return response.Value.VersionId;
	}


	/// <inheritdoc/>
	public async Task<string?> UploadStreamAsync(
		string containerId,
		string blobId,
		Stream stream,
		bool overwrite,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		BlobContentInfo response = await blobClient.UploadAsync(stream, overwrite, token);
		return response.VersionId;
	}

	/// <inheritdoc/>
	public async Task<string?> UploadStreamAsync(
		string containerId,
		string blobId,
		Stream stream,
		UploadBlobOptions options,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);

		BlobRequestConditions? conditions = null;
		if (options.RequestConditions is not null) {
			conditions = options.RequestConditions.AsAzureBlobRequestConditions();
		}

		BlobUploadOptions uploadOptions = new() {
			Conditions = conditions,
			Metadata = options.Metadata,
			Tags = options.Tags,
			ProgressHandler = options.ProgressHandler,
			TransferOptions = options.TransferOptions.AsAzureTransferOptions(),
			TransferValidation = options.TransferValidation.AsAzureUploadTransferValidation()
		};

		var response = await blobClient.UploadAsync(stream, uploadOptions, token);
		return response.Value.VersionId;
	}

	/// <inheritdoc/>
	public async Task<bool> ExistsAsync(
		string containerId,
		string blobId,
		CancellationToken token = default) {
		var containerClient = client.GetBlobContainerClient(containerId);
		var blobClient = containerClient.GetBlobClient(blobId);
		return await blobClient.ExistsAsync(token);
	}

}