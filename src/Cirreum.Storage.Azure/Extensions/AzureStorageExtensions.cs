namespace Cirreum.Storage.Extensions;

using Azure;
using Cirreum.Storage;

internal static class AzureStorageExtensions {

	public static BlobRequestConditions AsAzureBlobRequestConditions(
		this RequestBlobConditions conditions) {

		BlobRequestConditions requestCondition = new();

		requestCondition.WithAzureRequestConditions(conditions);

		if (string.IsNullOrWhiteSpace(conditions.LeaseId) is false) {
			conditions.LeaseId = conditions.LeaseId;
		}
		if (string.IsNullOrWhiteSpace(conditions.TagConditions) is false) {
			conditions.TagConditions = conditions.TagConditions;
		}

		return requestCondition;

	}

	public static RequestConditions AsAzureRequestConditions(
		this BlobConditions conditions) {
		RequestConditions requestConditions = new();
		requestConditions.WithAzureRequestConditions(conditions);
		return requestConditions;
	}
	public static void WithAzureRequestConditions(
		this RequestConditions requestConditions, BlobConditions conditions) {

		if (string.IsNullOrWhiteSpace(conditions.IfMatch) is false) {
			requestConditions.IfMatch = new(conditions.IfMatch);
		}

		if (string.IsNullOrWhiteSpace(conditions.IfNoneMatch) is false) {
			requestConditions.IfNoneMatch = new(conditions.IfNoneMatch);
		}

		if (conditions.IfModifiedSince.HasValue) {
			requestConditions.IfModifiedSince = conditions.IfModifiedSince;
		}

		if (conditions.IfUnmodifiedSince.HasValue) {
			requestConditions.IfUnmodifiedSince = conditions.IfUnmodifiedSince;
		}

	}

	public static Azure.Storage.StorageTransferOptions AsAzureTransferOptions(
		this TransferOptions options) {
		return new() {
			InitialTransferSize = options.InitialTransferLength,
			MaximumConcurrency = options.MaximumConcurrency,
			MaximumTransferSize = options.MaximumTransferSize,
		};
	}

	public static Azure.Storage.UploadTransferValidationOptions? AsAzureUploadTransferValidation(
		this TransferValidationOptions? options) {
		if (options is null) {
			return null;
		}
		return new() {
			ChecksumAlgorithm = options.ChecksumAlgorithm switch {
				StorageChecksumAlgorithm.None => Azure.Storage.StorageChecksumAlgorithm.None,
				StorageChecksumAlgorithm.Auto => Azure.Storage.StorageChecksumAlgorithm.Auto,
				StorageChecksumAlgorithm.StorageCrc64 => Azure.Storage.StorageChecksumAlgorithm.StorageCrc64,
				StorageChecksumAlgorithm.MD5 => Azure.Storage.StorageChecksumAlgorithm.MD5,
				_ => throw new NotImplementedException()
			}
		};
	}

	public static Azure.Storage.DownloadTransferValidationOptions? AsAzureDownloadTransferValidation(
		this TransferValidationOptions? options) {
		if (options is null) {
			return null;
		}
		return new() {
			ChecksumAlgorithm = options.ChecksumAlgorithm switch {
				StorageChecksumAlgorithm.None => Azure.Storage.StorageChecksumAlgorithm.None,
				StorageChecksumAlgorithm.Auto => Azure.Storage.StorageChecksumAlgorithm.Auto,
				StorageChecksumAlgorithm.StorageCrc64 => Azure.Storage.StorageChecksumAlgorithm.StorageCrc64,
				StorageChecksumAlgorithm.MD5 => Azure.Storage.StorageChecksumAlgorithm.MD5,
				_ => throw new NotImplementedException()
			}
		};
	}

}