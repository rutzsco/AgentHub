using AgentHub.Api.Agents;
using AgentHub.Api.Models;
using System.Text.Json;

namespace AgentHub.Api.Extensions;

public static class AgentEndpointExtensions
{
    /// <summary>
    /// Maps agent-related endpoints
    /// </summary>
    public static WebApplication MapAgentEndpoints(this WebApplication app)
    {
        // Image Analysis Agent endpoint
        app.MapPost("/agents/image-analysis/streaming", async (ChatThreadRequest request, ImageAnalysisAgent agent, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message is required" });
            }

            // Create a response that streams the chunks
            return Results.Stream(async stream =>
            {
                await foreach (var chunk in agent.ReplyPlannerAsync(request, cancellationToken))
                {
                    var json = JsonSerializer.Serialize(chunk);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json + "\n");
                    await stream.WriteAsync(bytes, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }
            }, contentType: "application/x-ndjson");
        })
        .WithName("ImageAnalysis")
        .WithOpenApi()
        .WithSummary("Analyze images using Azure OpenAI")
        .WithDescription("Processes image analysis requests using the ImageAnalysisAgent with Azure OpenAI chat completion. Returns streaming NDJSON responses.");

        // Image Analysis Agent endpoint (non-streaming)
        app.MapPost("/agents/image-analysis", async (ChatThreadRequest request, ImageAnalysisAgent agent, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.BadRequest(new { error = "Message is required" });
            }

            var chunks = new List<ChatChunkResponse>();
            await foreach (var chunk in agent.ReplyPlannerAsync(request, cancellationToken))
            {
                chunks.Add(chunk);
            }

            // Return the final result if available, otherwise all chunks
            var finalChunk = chunks.LastOrDefault(c => c.FinalResult != null);
            if (finalChunk?.FinalResult != null)
            {
                return Results.Ok(finalChunk.FinalResult);
            }

            return Results.Ok(new { chunks });
        })
        .WithName("ImageAnalysisComplete")
        .WithOpenApi()
        .WithSummary("Analyze images using Azure OpenAI (complete response)")
        .WithDescription("Processes image analysis requests using the ImageAnalysisAgent with Azure OpenAI chat completion. Returns the complete response after processing all chunks.");

        return app;
    }
}