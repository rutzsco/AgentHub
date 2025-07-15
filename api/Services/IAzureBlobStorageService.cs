namespace AgentHub.Api.Services;

/// <summary>
/// Service interface for Azure Blob Storage operations
/// </summary>
public interface IAzureBlobStorageService
{
    /// <summary>
    /// Download blob content as binary data
    /// </summary>
    /// <param name="blobName">Name of the blob to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Binary data and content type</returns>
    Task<(BinaryData Data, string ContentType)> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a blob exists
    /// </summary>
    /// <param name="blobName">Name of the blob to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blob exists, false otherwise</returns>
    Task<bool> BlobExistsAsync(string blobName, CancellationToken cancellationToken = default);
}
