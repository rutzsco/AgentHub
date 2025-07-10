using System.ComponentModel.DataAnnotations;

namespace AgentHub.Api.Models;

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