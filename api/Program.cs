using AgentHub.Api.Models;
using AgentHub.Api.Services;
using AgentHub.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Azure services using extension method
builder.Services.AddAzureServices(builder.Configuration);

// Register agents
builder.Services.AddAgents();

// Register Azure Search Service
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>();

// Register Azure OpenAI Service
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Redirect root path to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

// Status endpoint
app.MapGet("/status", () => Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow }))
    .WithName("GetStatus")
    .WithOpenApi();

// Knowledge endpoint
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

// Map agent endpoints
app.MapAgentEndpoints();

app.Run();
