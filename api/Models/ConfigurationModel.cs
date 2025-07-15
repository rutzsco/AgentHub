using System.ComponentModel.DataAnnotations;

namespace AgentHub.Api.Models;

/// <summary>
/// Configuration options for Azure credential settings
/// </summary>
public class AzureCredentialOptions
{
    public const string SectionName = "AzureCredential";
    
    /// <summary>
    /// Tenant ID for Visual Studio authentication
    /// </summary>
    public string? VisualStudioTenantId { get; set; }
    
    /// <summary>
    /// Client ID for Managed Identity (optional)
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }
    
    /// <summary>
    /// Exclude Visual Studio credential from the authentication chain
    /// </summary>
    public bool ExcludeVisualStudioCredential { get; set; } = false;
    
    /// <summary>
    /// Exclude Azure CLI credential from the authentication chain
    /// </summary>
    public bool ExcludeAzureCliCredential { get; set; } = false;
    
    /// <summary>
    /// Exclude environment credential from the authentication chain
    /// </summary>
    public bool ExcludeEnvironmentCredential { get; set; } = false;
    
    /// <summary>
    /// Exclude managed identity credential from the authentication chain
    /// </summary>
    public bool ExcludeManagedIdentityCredential { get; set; } = false;
    
    /// <summary>
    /// Exclude Azure PowerShell credential from the authentication chain
    /// </summary>
    public bool ExcludeAzurePowerShellCredential { get; set; } = false;
    
    /// <summary>
    /// Exclude interactive browser credential from the authentication chain
    /// </summary>
    public bool ExcludeInteractiveBrowserCredential { get; set; } = true; // Default to true for non-interactive scenarios
    
    /// <summary>
    /// Gets whether any Visual Studio tenant ID is configured
    /// </summary>
    public bool HasVisualStudioTenantId => !string.IsNullOrWhiteSpace(VisualStudioTenantId);
    
    /// <summary>
    /// Gets whether any managed identity client ID is configured
    /// </summary>
    public bool HasManagedIdentityClientId => !string.IsNullOrWhiteSpace(ManagedIdentityClientId);
    
    /// <summary>
    /// Validates the credential configuration
    /// </summary>
    public void Validate()
    {
        // If Visual Studio tenant ID is provided, it should be a valid GUID
        if (!string.IsNullOrWhiteSpace(VisualStudioTenantId) && !Guid.TryParse(VisualStudioTenantId, out _))
        {
            throw new ArgumentException($"Invalid VisualStudioTenantId format: {VisualStudioTenantId}. Must be a valid GUID.");
        }
        
        // If Managed Identity client ID is provided, it should be a valid GUID
        if (!string.IsNullOrWhiteSpace(ManagedIdentityClientId) && !Guid.TryParse(ManagedIdentityClientId, out _))
        {
            throw new ArgumentException($"Invalid ManagedIdentityClientId format: {ManagedIdentityClientId}. Must be a valid GUID.");
        }
    }
}

/// <summary>
/// Configuration options for Azure OpenAI service
/// </summary>
public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";
    
    /// <summary>
    /// Azure OpenAI service endpoint URL
    /// </summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the chat completion deployment
    /// </summary>
    public string ChatDeploymentName { get; set; } = "gpt-4";
    
    /// <summary>
    /// Name of the embedding deployment
    /// </summary>
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";
    
    /// <summary>
    /// Maximum text length for embeddings processing
    /// </summary>
    [Range(1000, 100000)]
    public int MaxTextLength { get; set; } = 30000;
    
    /// <summary>
    /// Enable retry logic for failed requests
    /// </summary>
    public bool EnableRetry { get; set; } = true;
    
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Validates that the endpoint is a valid URI and API key is not empty
    /// </summary>
    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Invalid Azure OpenAI endpoint: {Endpoint}");
        }
        
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ArgumentException("Azure OpenAI API key cannot be empty");
        }
    }
}

/// <summary>
/// Configuration options for Azure Search service
/// </summary>
public class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";
    
    /// <summary>
    /// Azure Search service endpoint URL
    /// </summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure Search admin API key
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Vector dimensions for embeddings (default for text-embedding-ada-002)
    /// </summary>
    [Range(100, 10000)]
    public int VectorDimensions { get; set; } = 1536;
    
    /// <summary>
    /// Vector search profile name
    /// </summary>
    public string VectorSearchProfile { get; set; } = "default-vector-profile";
    
    /// <summary>
    /// Vector search algorithm name
    /// </summary>
    public string VectorSearchAlgorithm { get; set; } = "default-vector-algorithm";
    
    /// <summary>
    /// Enable retry logic for failed requests
    /// </summary>
    public bool EnableRetry { get; set; } = true;
    
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Validates that the endpoint is a valid URI and API key is not empty
    /// </summary>
    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"Invalid Azure Search endpoint: {Endpoint}");
        }
        
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ArgumentException("Azure Search API key cannot be empty");
        }
    }
}

/// <summary>
/// Configuration options for Azure Blob Storage service
/// </summary>
public class AzureBlobStorageOptions
{
    public const string SectionName = "AzureBlobStorage";
    
    /// <summary>
    /// Azure Storage account key or connection string
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Default container name for storing images
    /// </summary>
    public string ContainerName { get; set; } = "images";
    
    /// <summary>
    /// Enable retry logic for failed requests
    /// </summary>
    public bool EnableRetry { get; set; } = true;
    
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Gets whether blob storage is configured
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint);
    
    /// <summary>
    /// Validates that the connection string and container name are not empty
    /// </summary>
    public void Validate()
    {
        if (!IsConfigured)
        {
            return; // Skip validation if not configured
        }
        
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new ArgumentException("Azure Blob Storage endpoint string cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(ContainerName))
        {
            throw new ArgumentException("Azure Blob Storage container name cannot be empty");
        }
    }
}