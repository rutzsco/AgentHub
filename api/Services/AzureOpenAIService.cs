using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using AgentHub.Api.Models;
using OpenAI.Embeddings;

namespace AgentHub.Api.Services;

public interface IAzureOpenAIService
{
    Task<float[]> GetEmbeddingsAsync(string text);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AzureOpenAIOptions _options;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(IOptions<AzureOpenAIOptions> options, ILogger<AzureOpenAIService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            throw new InvalidOperationException("Azure OpenAI configuration is missing. Please provide Endpoint and ApiKey.");
        }
        
        _openAIClient = new AzureOpenAIClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<float[]> GetEmbeddingsAsync(string text)
    {
        try
        {
            _logger.LogDebug("Generating embeddings for text of length: {TextLength}", text.Length);
            
            // Clean and truncate text if needed (Azure OpenAI has token limits)
            var cleanedText = CleanText(text);
            
            // Get the embedding client for the specific deployment
            var embeddingClient = _openAIClient.GetEmbeddingClient(_options.EmbeddingDeploymentName);
            
            var response = await embeddingClient.GenerateEmbeddingAsync(cleanedText);
            
            var embeddings = response.Value.ToFloats().ToArray();
            
            _logger.LogDebug("Successfully generated embeddings with {Dimensions} dimensions", embeddings.Length);
            
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for text");
            throw;
        }
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        
        // Remove excessive whitespace and normalize
        text = text.Trim();
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        
        // Truncate if too long (use configurable max length)
        if (text.Length > _options.MaxTextLength)
        {
            text = text.Substring(0, _options.MaxTextLength);
            _logger.LogWarning("Text truncated from {OriginalLength} to {MaxLength} characters", 
                text.Length + (_options.MaxTextLength - text.Length), _options.MaxTextLength);
        }
        
        return text;
    }
}
