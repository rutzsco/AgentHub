using AgentHub.Api.Extensions;
using AgentHub.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AgentHub.Api.Agents;

internal sealed class ImageAnalysisAgent
{
    private readonly ILogger<ImageAnalysisAgent> _logger;
    private readonly AzureOpenAIOptions _azureOpenAIOptions;

    public ImageAnalysisAgent(ILogger<ImageAnalysisAgent> logger,
                              IOptions<AzureOpenAIOptions> azureOpenAIOptions)
    {
        _logger = logger;
        _azureOpenAIOptions = azureOpenAIOptions.Value;
    }

    public async IAsyncEnumerable<ChatChunkResponse> ReplyPlannerAsync(ChatThreadRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        // Kernel setup
        var kernel = Kernel.CreateBuilder()
           .AddAzureOpenAIChatCompletion(
               deploymentName: _azureOpenAIOptions.ChatDeploymentName, 
               endpoint: _azureOpenAIOptions.Endpoint, 
               apiKey: _azureOpenAIOptions.ApiKey)
           .Build();

        var chatGpt = kernel.Services.GetService<IChatCompletionService>();
        ArgumentNullException.ThrowIfNull(chatGpt, nameof(chatGpt));

        // Build Chat History
        var chatHistory = new ChatHistory(PromptUtilities.GetPromptByName("ImageAnalysisAgentInstructions"));
        
        // Build message content collection to support multimodal input
        var messageContent = new ChatMessageContentItemCollection();
        
        // Add text message
        messageContent.Add(new TextContent(request.Message));
        
        // Add images if present
        if (request.Files != null)
        {
            foreach (var file in request.Files)
            {
                try
                {
                    // Parse data URL to extract media type and base64 data
                    if (file.DataUrl.StartsWith("data:"))
                    {
                        var dataParts = file.DataUrl.Split(',');
                        if (dataParts.Length == 2)
                        {
                            var headerPart = dataParts[0]; // e.g., "data:image/jpeg;base64"
                            var base64Data = dataParts[1];
                            
                            // Extract media type
                            var mediaType = headerPart.Split(';')[0].Replace("data:", "");
                            
                            // Convert base64 to bytes
                            var imageBytes = Convert.FromBase64String(base64Data);
                            var binaryData = new BinaryData(imageBytes);
                            
                            // Add image content
                            messageContent.Add(new ImageContent(binaryData, mediaType));
                            
                            _logger.LogInformation("Added image {FileName} ({MediaType}, {Size} bytes) to chat history", 
                                file.Name, mediaType, imageBytes.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process image file {FileName}", file.Name);
                }
            }
        }
        
        // Add user message with text and images
        chatHistory.Add(new ChatMessageContent(AuthorRole.User, messageContent));

        // Execute Chat Completion
        var executionSettings = new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
        var sb = new StringBuilder();
        await foreach (StreamingChatMessageContent responseChunk in chatGpt.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken))
        {
            if (responseChunk.Content != null)
            {
                sb.Append(responseChunk.Content);
                yield return new ChatChunkResponse(ChatChunkContentType.Text, responseChunk.Content);
                await Task.Yield();
            }
        }
        sw.Stop();

        yield return new ChatChunkResponse(ChatChunkContentType.Text, string.Empty, new ChatChunkResponseResult(sb.ToString()));
    }
}