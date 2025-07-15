using AgentHub.Api.Models;
using AgentHub.Api.Services;

namespace AgentHub.Api.Extensions;

public static class KnowledgeEndpointExtensions
{
    /// <summary>
    /// Maps knowledge-related endpoints
    /// </summary>
    public static WebApplication MapKnowledgeEndpoints(this WebApplication app)
    {
        // Knowledge indexing endpoint
        app.MapPost("/knowledge", async (KnowledgeRequest request, IAzureSearchService searchService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { error = "Content is required" });
            }
            
            if (string.IsNullOrWhiteSpace(request.IndexName))
            {
                return Results.BadRequest(new { error = "Index name is required" });
            }
            
            var response = await searchService.IndexKnowledgeAsync(request);
            
            return response.Status == "Success" 
                ? Results.Ok(response) 
                : Results.BadRequest(response);
        })
        .WithName("PostKnowledge")
        .WithOpenApi()
        .WithSummary("Index knowledge to Azure AI Search")
        .WithDescription("Posts knowledge content to a specified Azure AI Search index with optional security filters and metadata");

        // Knowledge search endpoint
        app.MapPost("/knowledge/search", async (KnowledgeSearchRequest request, IAzureSearchService searchService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return Results.BadRequest(new { error = "Search query is required" });
            }
            
            if (string.IsNullOrWhiteSpace(request.IndexName))
            {
                return Results.BadRequest(new { error = "Index name is required" });
            }
            
            if (request.Top <= 0 || request.Top > 100)
            {
                return Results.BadRequest(new { error = "Top must be between 1 and 100" });
            }
            
            var response = await searchService.SearchKnowledgeAsync(request);
            
            return response.Status == "Success" 
                ? Results.Ok(response) 
                : Results.BadRequest(response);
        })
        .WithName("SearchKnowledge")
        .WithOpenApi()
        .WithSummary("Search knowledge using hybrid vector search")
        .WithDescription("Performs hybrid search combining category filtering with vector similarity ranking on knowledge content in a specified Azure AI Search index");

        return app;
    }
}