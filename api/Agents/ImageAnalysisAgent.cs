using AgentHub.Api.Extensions;
using AgentHub.Api.Models;
using AgentHub.Api.Services;
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
    private readonly IAzureBlobStorageService? _blobStorageService;

    public ImageAnalysisAgent(ILogger<ImageAnalysisAgent> logger,
                              IOptions<AzureOpenAIOptions> azureOpenAIOptions,
                              IAzureBlobStorageService? blobStorageService = null)
    {
        _logger = logger;
        _azureOpenAIOptions = azureOpenAIOptions.Value;
        _blobStorageService = blobStorageService;
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
                    BinaryData? imageData = null;
                    string? mediaType = null;

                    // Handle data URL
                    if (!string.IsNullOrWhiteSpace(file.DataUrl))
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
                                mediaType = headerPart.Split(';')[0].Replace("data:", "");
                                
                                // Convert base64 to bytes
                                var imageBytes = Convert.FromBase64String(base64Data);
                                imageData = new BinaryData(imageBytes);
                                
                                _logger.LogInformation("Processed data URL for image {FileName} ({MediaType}, {Size} bytes)", 
                                    file.Name, mediaType, imageBytes.Length);
                            }
                        }
                    }
                    // Handle blob name
                    else if (!string.IsNullOrWhiteSpace(file.BlobName))
                    {
                        if (_blobStorageService == null)
                        {
                            _logger.LogWarning("Blob storage service not configured, skipping blob {BlobName} for file {FileName}", file.BlobName, file.Name);
                            continue;
                        }

                        // Check if blob exists
                        var blobExists = await _blobStorageService.BlobExistsAsync(file.BlobName, cancellationToken);
                        if (!blobExists)
                        {
                            _logger.LogWarning("Blob {BlobName} does not exist, skipping file {FileName}", file.BlobName, file.Name);
                            continue;
                        }

                        // Download blob content
                        var (blobData, contentType) = await _blobStorageService.DownloadBlobAsync(file.BlobName, cancellationToken);
                        imageData = blobData;
                        mediaType = contentType;
                        
                        _logger.LogInformation("Downloaded blob {BlobName} for image {FileName} ({MediaType}, {Size} bytes)", 
                            file.BlobName, file.Name, mediaType, blobData.ToArray().Length);
                    }
                    else
                    {
                        _logger.LogWarning("File {FileName} has neither DataUrl nor BlobName specified", file.Name);
                        continue;
                    }

                    // Add image content if we have valid data
                    if (imageData != null && !string.IsNullOrWhiteSpace(mediaType))
                    {
                        messageContent.Add(new ImageContent(imageData, mediaType));
                        _logger.LogInformation("Added image {FileName} to chat message", file.Name);
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