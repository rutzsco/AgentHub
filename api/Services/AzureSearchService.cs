using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using AgentHub.Api.Models;
using System.Text.Json;

namespace AgentHub.Api.Services;

public interface IAzureSearchService
{
    Task<KnowledgeResponse> IndexKnowledgeAsync(KnowledgeRequest request);
    Task<KnowledgeSearchResponse> SearchKnowledgeAsync(KnowledgeSearchRequest request);
    Task<bool> EnsureIndexExistsAsync(string indexName);
}

public class AzureSearchService : IAzureSearchService
{
    private readonly SearchIndexClient _indexClient;
    private readonly AzureSearchOptions _options;
    private readonly ILogger<AzureSearchService> _logger;
    private readonly IAzureOpenAIService _openAIService;

    public AzureSearchService(IOptions<AzureSearchOptions> options, ILogger<AzureSearchService> logger, IAzureOpenAIService openAIService)
    {
        _options = options.Value;
        _logger = logger;
        _openAIService = openAIService;
        
        if (string.IsNullOrEmpty(_options.Endpoint) || string.IsNullOrEmpty(_options.ApiKey))
        {
            throw new InvalidOperationException("Azure Search configuration is missing. Please provide Endpoint and ApiKey.");
        }
        
        _indexClient = new SearchIndexClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
    }

    public async Task<KnowledgeResponse> IndexKnowledgeAsync(KnowledgeRequest request)
    {
        try
        {
            _logger.LogInformation("Starting to index knowledge to index: {IndexName}", request.IndexName);
            
            // Ensure the index exists
            var indexExists = await EnsureIndexExistsAsync(request.IndexName);
            if (!indexExists)
            {
                var errorMessage = $"Failed to create or verify index: {request.IndexName}";
                _logger.LogError(errorMessage);
                return new KnowledgeResponse
                {
                    Id = string.Empty,
                    Status = "Error",
                    Message = errorMessage
                };
            }
            
            // Generate embeddings for the content
            _logger.LogDebug("Generating embeddings for content");
            var embeddings = await _openAIService.GetEmbeddingsAsync(request.Content);
            
            // Create the document
            var document = new KnowledgeDocument
            {
                Content = request.Content,
                Title = request.Title,
                Category = request.Category,
                SecurityFilters = request.SecurityFilters,
                Metadata = request.Metadata
            };
            
            // Get the search client for the specific index
            var searchClient = _indexClient.GetSearchClient(request.IndexName);
            
            // Convert the document to a dictionary for indexing
            var documentDict = ConvertToSearchDocument(document, embeddings);
            
            // Index the document
            var indexResponse = await searchClient.IndexDocumentsAsync(
                IndexDocumentsBatch.Upload(new[] { documentDict }));
            
            if (indexResponse.Value.Results.Any(r => !r.Succeeded))
            {
                var failedResult = indexResponse.Value.Results.First(r => !r.Succeeded);
                throw new InvalidOperationException($"Failed to index document: {failedResult.ErrorMessage}");
            }
            
            _logger.LogInformation("Successfully indexed knowledge document with ID: {DocumentId}", document.Id);
            
            return new KnowledgeResponse
            {
                Id = document.Id,
                Status = "Success",
                Message = "Knowledge successfully indexed with vector embeddings"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing knowledge to index: {IndexName}", request.IndexName);
            
            return new KnowledgeResponse
            {
                Id = string.Empty,
                Status = "Error",
                Message = ex.Message
            };
        }
    }

    public async Task<KnowledgeSearchResponse> SearchKnowledgeAsync(KnowledgeSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Starting hybrid search in index: {IndexName} with query: {Query}", 
                request.IndexName, request.Query);
            
            // Ensure the index exists before searching
            var indexExists = await EnsureIndexExistsAsync(request.IndexName);
            if (!indexExists)
            {
                var errorMessage = $"Failed to create or verify index: {request.IndexName}";
                _logger.LogError(errorMessage);
                return new KnowledgeSearchResponse
                {
                    Results = new List<KnowledgeSearchResult>(),
                    TotalCount = 0,
                    Status = "Error",
                    Message = errorMessage,
                    Query = request.Query
                };
            }
            
            // Generate embeddings for the search query
            _logger.LogDebug("Generating embeddings for search query");
            var queryEmbeddings = await _openAIService.GetEmbeddingsAsync(request.Query);
            
            // Get the search client for the specific index
            var searchClient = _indexClient.GetSearchClient(request.IndexName);
            
            // Build search options for hybrid search (text + vector)
            var searchOptions = new SearchOptions
            {
                Size = request.Top,
                IncludeTotalCount = true,
                QueryType = SearchQueryType.Full
            };
            
            // Add vector queries for semantic search
            var vectorQuery = new VectorizedQuery(queryEmbeddings)
            {
                KNearestNeighborsCount = request.Top * 2, // Get more vector results for better hybrid ranking
                Fields = { "text_vector" }
            };
            
            searchOptions.VectorSearch = new()
            {
                Queries = { vectorQuery }
            };
            
            // Add select fields (exclude vector field as it's not needed in response)
            searchOptions.Select.Add("id");
            searchOptions.Select.Add("content");
            searchOptions.Select.Add("title");
            searchOptions.Select.Add("category");
            searchOptions.Select.Add("createdAt");
            searchOptions.Select.Add("updatedAt");
            searchOptions.Select.Add("metadata");
            
            // Build comprehensive RBAC filter expression
            var filters = new List<string>();
            
            // Add category filters
            if (request.Categories != null && request.Categories.Length > 0)
            {
                var categoryFilter = string.Join(" or ", 
                    request.Categories.Select(c => $"category eq '{EscapeODataString(c)}'"));
                filters.Add($"({categoryFilter})");
            }
            
            // Enhanced security filters for RBAC
            if (request.SecurityFilters != null && request.SecurityFilters.Any())
            {
                var securityFilterGroups = new List<string>();
                
                foreach (var filter in request.SecurityFilters)
                {
                    var filterKey = EscapeODataString(filter.Key);
                    
                    if (filter.Value is string stringValue)
                    {
                        var escapedValue = EscapeODataString(stringValue);
                        securityFilterGroups.Add($"securityFilters/any(sf: sf eq '{filterKey}:{escapedValue}')");
                    }
                    else if (filter.Value is string[] arrayValue)
                    {
                        var arrayFilters = arrayValue.Select(v => 
                            $"securityFilters/any(sf: sf eq '{filterKey}:{EscapeODataString(v)}')");
                        securityFilterGroups.Add($"({string.Join(" or ", arrayFilters)})");
                    }
                    else if (filter.Value != null)
                    {
                        var escapedValue = EscapeODataString(filter.Value.ToString()!);
                        securityFilterGroups.Add($"securityFilters/any(sf: sf eq '{filterKey}:{escapedValue}')");
                    }
                }
                
                if (securityFilterGroups.Any())
                {
                    filters.Add($"({string.Join(" and ", securityFilterGroups)})");
                }
            }
            
            if (filters.Any())
            {
                searchOptions.Filter = string.Join(" and ", filters);
            }
            
            // Perform hybrid search (text + vector)
            string searchQuery = request.Query;
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = "*"; // Search all if no query provided
            }
            
            var searchResults = await searchClient.SearchAsync<SearchDocument>(searchQuery, searchOptions);
            
            var results = new List<KnowledgeSearchResult>();
            
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                var searchResult = new KnowledgeSearchResult
                {
                    Id = result.Document.GetString("id") ?? string.Empty,
                    Title = result.Document.GetString("title"),
                    Category = result.Document.GetString("category"),
                    CreatedAt = result.Document.GetDateTimeOffset("createdAt")?.DateTime ?? DateTime.MinValue,
                    UpdatedAt = result.Document.GetDateTimeOffset("updatedAt")?.DateTime ?? DateTime.MinValue,
                    Score = result.Score ?? 0.0 // Use Azure Search's hybrid ranking score
                };
                
                // Include content if requested
                if (request.IncludeContent)
                {
                    searchResult.Content = result.Document.GetString("content") ?? string.Empty;
                }
                
                // Parse metadata if available
                var metadataJson = result.Document.GetString("metadata");
                if (!string.IsNullOrEmpty(metadataJson))
                {
                    try
                    {
                        searchResult.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse metadata JSON for document {DocumentId}", searchResult.Id);
                    }
                }
                
                results.Add(searchResult);
            }
            
            _logger.LogInformation("Successfully completed hybrid search. Found {ResultCount} results", results.Count);
            
            return new KnowledgeSearchResponse
            {
                Results = results,
                TotalCount = (int)(searchResults.Value.TotalCount ?? 0),
                Status = "Success",
                Message = $"Found {results.Count} results using hybrid search with vector similarity",
                Query = request.Query
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching knowledge in index: {IndexName}", request.IndexName);
            
            return new KnowledgeSearchResponse
            {
                Results = new List<KnowledgeSearchResult>(),
                TotalCount = 0,
                Status = "Error",
                Message = ex.Message,
                Query = request.Query
            };
        }
    }

    public async Task<bool> EnsureIndexExistsAsync(string indexName)
    {
        try
        {
            _logger.LogInformation("Checking if index exists: {IndexName}", indexName);
            
            // Check if index exists
            try
            {
                var existingIndex = await _indexClient.GetIndexAsync(indexName);
                _logger.LogInformation("Index {IndexName} already exists", indexName);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Index doesn't exist, create it
                _logger.LogInformation("Index {IndexName} does not exist. Creating it.", indexName);
                await CreateIndexAsync(indexName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if index exists: {IndexName}. Exception type: {ExceptionType}, Message: {Message}", 
                    indexName, ex.GetType().Name, ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in EnsureIndexExistsAsync for index: {IndexName}. Exception type: {ExceptionType}, Message: {Message}", 
                indexName, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    private async Task CreateIndexAsync(string indexName)
    {
        try
        {
            _logger.LogInformation("Starting to create index: {IndexName}", indexName);
            
            // Define vector search configuration using options
            var vectorSearchProfile = new VectorSearchProfile(_options.VectorSearchProfile, _options.VectorSearchAlgorithm);
            var vectorSearchAlgorithm = new HnswAlgorithmConfiguration(_options.VectorSearchAlgorithm);
            
            var vectorSearch = new VectorSearch
            {
                Profiles = { vectorSearchProfile },
                Algorithms = { vectorSearchAlgorithm }
            };
            
            var fields = new List<SearchField>
            {
                new SearchField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchField("content", SearchFieldDataType.String) { IsFilterable = true },
                new SearchField("title", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SearchField("category", SearchFieldDataType.String) { IsSearchable = true, IsFilterable = true, IsFacetable = true },
                new SearchField("createdAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SearchField("updatedAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SearchField("securityFilters", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true },
                new SearchField("metadata", SearchFieldDataType.String) { IsFilterable = true },
                // Vector field with configurable dimensions
                new VectorSearchField("text_vector", _options.VectorDimensions, _options.VectorSearchProfile)
            };

            _logger.LogDebug("Created field definitions with {FieldCount} fields and {VectorDimensions} vector dimensions", 
                fields.Count, _options.VectorDimensions);

            var definition = new SearchIndex(indexName, fields)
            {
                VectorSearch = vectorSearch
            };
            
            _logger.LogDebug("Created index definition for: {IndexName}", indexName);
            
            var response = await _indexClient.CreateIndexAsync(definition);
            
            _logger.LogInformation("Successfully created index: {IndexName} with status: {StatusCode}", 
                indexName, response.GetRawResponse().Status);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Search RequestFailedException while creating index: {IndexName}. " +
                "Status: {Status}, ErrorCode: {ErrorCode}, Message: {Message}, Content: {Content}", 
                indexName, ex.Status, ex.ErrorCode, ex.Message, ex.GetRawResponse()?.Content?.ToString());
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "ArgumentException while creating index: {IndexName}. This usually means invalid field configuration. Message: {Message}", 
                indexName, ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "InvalidOperationException while creating index: {IndexName}. This usually means service configuration issues. Message: {Message}", 
                indexName, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while creating index: {IndexName}. Exception type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                indexName, ex.GetType().Name, ex.Message, ex.StackTrace);
            throw;
        }
    }

    private static Dictionary<string, object> ConvertToSearchDocument(KnowledgeDocument document, float[] embeddings)
    {
        var searchDocument = new Dictionary<string, object>
        {
            ["id"] = document.Id,
            ["content"] = document.Content,
            ["title"] = document.Title ?? string.Empty,
            ["category"] = document.Category ?? string.Empty,
            ["createdAt"] = document.CreatedAt,
            ["updatedAt"] = document.UpdatedAt,
            ["text_vector"] = embeddings
        };

        // Convert security filters to string array for better RBAC filtering
        var securityFiltersList = new List<string>();
        if (document.SecurityFilters != null)
        {
            foreach (var filter in document.SecurityFilters)
            {
                if (filter.Value is string stringValue)
                {
                    securityFiltersList.Add($"{filter.Key}:{stringValue}");
                }
                else if (filter.Value is string[] arrayValue)
                {
                    foreach (var value in arrayValue)
                    {
                        securityFiltersList.Add($"{filter.Key}:{value}");
                    }
                }
                else if (filter.Value != null)
                {
                    securityFiltersList.Add($"{filter.Key}:{filter.Value}");
                }
            }
        }
        searchDocument["securityFilters"] = securityFiltersList.ToArray();

        // Serialize metadata as JSON string
        if (document.Metadata != null)
        {
            searchDocument["metadata"] = JsonSerializer.Serialize(document.Metadata);
        }
        else
        {
            searchDocument["metadata"] = string.Empty;
        }

        return searchDocument;
    }

    private static string EscapeODataString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        // Escape single quotes for OData filter expressions
        return input.Replace("'", "''");
    }
}
