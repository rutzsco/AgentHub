# AgentHub API Configuration

This document describes the maintainable configuration approach used in AgentHub API.

## Configuration Overview

The application uses the **Options pattern** for managing Azure service configurations, which provides several benefits over direct string-based configuration access:

- **Type safety**: Configuration is bound to strongly-typed classes
- **Validation**: Built-in validation using Data Annotations and custom validation methods
- **Maintainability**: Centralized configuration management
- **Testability**: Easy to mock and test configuration values
- **Startup validation**: Configuration errors are caught at application startup

## File Structure

Configuration classes are organized as follows:

- `Models/ConfigurationModel.cs`: Contains Azure service configuration option classes
  - `AzureOpenAIOptions`: Configuration for Azure OpenAI service
  - `AzureSearchOptions`: Configuration for Azure Search service
- `Models/KnowledgeModels.cs`: Contains knowledge-related data models
- `Extensions/ServiceCollectionExtensions.cs`: Configuration setup and validation

## Azure Services Configuration

### Azure OpenAI Configuration
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "EmbeddingDeploymentName": "text-embedding-ada-002",
    "MaxTextLength": 30000,
    "EnableRetry": true,
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:02"
  }
}
**Configuration Properties:**
- `Endpoint` (Required): Azure OpenAI service endpoint URL
- `ApiKey` (Required): Azure OpenAI API key
- `EmbeddingDeploymentName`: Name of the embedding deployment (default: "text-embedding-ada-002")
- `MaxTextLength`: Maximum text length for embeddings (1000-100000, default: 30000)
- `EnableRetry`: Enable retry logic (default: true)
- `MaxRetryAttempts`: Maximum retry attempts (1-10, default: 3)
- `RetryDelay`: Delay between retries (default: 2 seconds)

### Azure Search Configuration
{
  "AzureSearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "ApiKey": "your-search-api-key-here",
    "VectorDimensions": 1536,
    "VectorSearchProfile": "default-vector-profile",
    "VectorSearchAlgorithm": "default-vector-algorithm",
    "EnableRetry": true,
    "MaxRetryAttempts": 3
  }
}
**Configuration Properties:**
- `Endpoint` (Required): Azure Search service endpoint URL
- `ApiKey` (Required): Azure Search admin API key
- `VectorDimensions`: Vector dimensions for embeddings (100-10000, default: 1536)
- `VectorSearchProfile`: Vector search profile name (default: "default-vector-profile")
- `VectorSearchAlgorithm`: Vector search algorithm name (default: "default-vector-algorithm")
- `EnableRetry`: Enable retry logic (default: true)
- `MaxRetryAttempts`: Maximum retry attempts (1-10, default: 3)

## Implementation Details

### Configuration Classes

The configuration is implemented using these classes in `Models/ConfigurationModel.cs`:

- `AzureOpenAIOptions`: Strongly-typed configuration for Azure OpenAI
- `AzureSearchOptions`: Strongly-typed configuration for Azure Search

Both classes include:
- Data annotation validation
- Custom validation methods
- Sensible default values
- Comprehensive XML documentation

### Service Registration

Services are registered using the extension method approach:
builder.Services.AddAzureServices(builder.Configuration);
This method:
- Binds configuration sections to option classes
- Validates configuration using data annotations
- Performs custom validation
- Validates configuration at startup

### Usage in Services

Services receive configuration through dependency injection:
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly AzureOpenAIOptions _options;
    
    public AzureOpenAIService(IOptions<AzureOpenAIOptions> options, ILogger<AzureOpenAIService> logger)
    {
        _options = options.Value;
        // ... rest of implementation
    }
}
## Environment-Specific Configuration

- `appsettings.json`: Contains default/template configuration
- `appsettings.Development.json`: Contains development environment configuration
- `appsettings.Production.json`: Should contain production environment configuration

## Security Considerations

- Never commit real API keys to source control
- Use Azure Key Vault or environment variables for production secrets
- Consider using Managed Identity for Azure services where possible

## Migration from String-Based Configuration

The previous implementation used direct `IConfiguration` access:
// Old approach
var endpoint = _configuration["AzureOpenAI:Endpoint"];
var apiKey = _configuration["AzureOpenAI:ApiKey"];
The new approach provides type safety and validation:
// New approach
public AzureOpenAIService(IOptions<AzureOpenAIOptions> options)
{
    _options = options.Value; // Strongly typed and validated
}
## Benefits

1. **Type Safety**: Compile-time checking of configuration properties
2. **Validation**: Automatic validation at startup prevents runtime errors
3. **Maintainability**: Changes to configuration are centralized
4. **Documentation**: Configuration properties are self-documenting
5. **Testing**: Easy to create test configurations
6. **IDE Support**: IntelliSense and refactoring support
7. **Organization**: Configuration classes are separated from business logic models