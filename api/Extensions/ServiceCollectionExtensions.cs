using Microsoft.Extensions.Options;
using AgentHub.Api.Models;

namespace AgentHub.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Azure services with the Options pattern
    /// </summary>
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Azure OpenAI options
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.AddOptions<AzureOpenAIOptions>()
            .Bind(configuration.GetSection(AzureOpenAIOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                try
                {
                    options.Validate();
                    return true;
                }
                catch
                {
                    return false;
                }
            }, "Azure OpenAI configuration validation failed")
            .ValidateOnStart();

        // Configure Azure Search options
        services.Configure<AzureSearchOptions>(configuration.GetSection(AzureSearchOptions.SectionName));
        services.AddOptions<AzureSearchOptions>()
            .Bind(configuration.GetSection(AzureSearchOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                try
                {
                    options.Validate();
                    return true;
                }
                catch
                {
                    return false;
                }
            }, "Azure Search configuration validation failed")
            .ValidateOnStart();

        return services;
    }
}