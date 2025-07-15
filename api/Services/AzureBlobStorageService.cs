using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AgentHub.Api.Models;
using Microsoft.Extensions.Options;

namespace AgentHub.Api.Services;

/// <summary>
/// Service for Azure Blob Storage operations
/// </summary>
public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly AzureBlobStorageOptions _options;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(
        ILogger<AzureBlobStorageService> logger,
        IOptions<AzureBlobStorageOptions> options,
        IAzureCredentialFactory credentialFactory)
    {
        _logger = logger;
        _options = options.Value;
        
        // Use centralized credential factory
        var credential = credentialFactory.CreateDefaultAzureCredential();
        
        _blobServiceClient = new BlobServiceClient(new Uri(_options.Endpoint), credential);
        _containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
    }

    /// <summary>
    /// Download blob content as binary data
    /// </summary>
    /// <param name="blobName">Name of the blob to download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Binary data and content type</returns>
    public async Task<(BinaryData Data, string ContentType)> DownloadBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading blob {BlobName} from container {ContainerName}", blobName, _options.ContainerName);
            
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            // Download blob content
            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var content = response.Value.Content;
            var contentType = response.Value.Details.ContentType ?? "application/octet-stream";
            
            _logger.LogInformation("Successfully downloaded blob {BlobName} ({Size} bytes, {ContentType})", 
                blobName, content.ToArray().Length, contentType);
            
            return (content, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob {BlobName} from container {ContainerName}", blobName, _options.ContainerName);
            throw;
        }
    }

    /// <summary>
    /// Check if a blob exists
    /// </summary>
    /// <param name="blobName">Name of the blob to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blob exists, false otherwise</returns>
    public async Task<bool> BlobExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync(cancellationToken);
            
            _logger.LogDebug("Blob {BlobName} exists: {Exists}", blobName, response.Value);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if blob {BlobName} exists in container {ContainerName}", blobName, _options.ContainerName);
            return false;
        }
    }
}
