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

// Register Azure Blob Storage Service (only if configured)
var blobStorageConfig = builder.Configuration.GetSection(AzureBlobStorageOptions.SectionName).Get<AzureBlobStorageOptions>();
if (blobStorageConfig?.IsConfigured == true)
{
    builder.Services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
}

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

// Map knowledge endpoints
app.MapKnowledgeEndpoints();

// Map agent endpoints
app.MapAgentEndpoints();

app.Run();
