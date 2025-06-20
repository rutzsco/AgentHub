using Azure;
using Azure.AI.OpenAI;
using System.Text.Json;

namespace AgentHub.Api.Services;

public interface IAzureOpenAIService
{
    Task<float[]> GetEmbeddingsAsync(string text);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly string _embeddingDeploymentName;

    public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        _embeddingDeploymentName = _configuration["AzureOpenAI:EmbeddingDeploymentName"] ?? "text-embedding-ada-002";
        
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Azure OpenAI configuration is missing. Please provide Endpoint and ApiKey.");
        }
        
        _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<float[]> GetEmbeddingsAsync(string text)
    {
        try
        {
            _logger.LogDebug("Generating embeddings for text of length: {TextLength}", text.Length);
            
            // Clean and truncate text if needed (Azure OpenAI has token limits)
            var cleanedText = CleanText(text);
            
            var embeddingsOptions = new EmbeddingsOptions(_embeddingDeploymentName, new[] { cleanedText });
            
            var response = await _openAIClient.GetEmbeddingsAsync(embeddingsOptions);
            
            if (response.Value.Data.Count == 0)
            {
                throw new InvalidOperationException("No embeddings returned from Azure OpenAI");
            }
            
            var embeddings = response.Value.Data[0].Embedding.ToArray();
            
            _logger.LogDebug("Successfully generated embeddings with {Dimensions} dimensions", embeddings.Length);
            
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for text");
            throw;
        }
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        
        // Remove excessive whitespace and normalize
        text = text.Trim();
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        
        // Truncate if too long (rough estimate for token limits)
        // Azure OpenAI text-embedding-ada-002 has ~8192 token limit
        // Rough estimate: 1 token ≈ 4 characters
        const int maxLength = 30000; // Conservative estimate
        if (text.Length > maxLength)
        {
            text = text.Substring(0, maxLength);
        }
        
        return text;
    }
}
