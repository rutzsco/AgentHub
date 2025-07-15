using Microsoft.Extensions.Options;
using AgentHub.Api.Models;
using AgentHub.Api.Agents;
using AgentHub.Api.Services;

namespace AgentHub.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Azure services with the Options pattern
    /// </summary>
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Azure Credential options with backward compatibility
        services.Configure<AzureCredentialOptions>(options =>
        {
            // Bind from the dedicated section first
            configuration.GetSection(AzureCredentialOptions.SectionName).Bind(options);
            
            // For backward compatibility, check if VisualStudioTenantId exists at root level
            var rootTenantId = configuration["VisualStudioTenantId"];
            if (!string.IsNullOrWhiteSpace(rootTenantId) && string.IsNullOrWhiteSpace(options.VisualStudioTenantId))
            {
                options.VisualStudioTenantId = rootTenantId;
            }
        });
        
        services.AddOptions<AzureCredentialOptions>()
            .Configure(options =>
            {
                // Bind from the dedicated section first
                configuration.GetSection(AzureCredentialOptions.SectionName).Bind(options);
                
                // For backward compatibility, check if VisualStudioTenantId exists at root level
                var rootTenantId = configuration["VisualStudioTenantId"];
                if (!string.IsNullOrWhiteSpace(rootTenantId) && string.IsNullOrWhiteSpace(options.VisualStudioTenantId))
                {
                    options.VisualStudioTenantId = rootTenantId;
                }
            })
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
            }, "Azure Credential configuration validation failed");

        // Register Azure credential factory
        services.AddSingleton<IAzureCredentialFactory, AzureCredentialFactory>();

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

        // Configure Azure Blob Storage options (optional)
        services.Configure<AzureBlobStorageOptions>(configuration.GetSection(AzureBlobStorageOptions.SectionName));
        services.AddOptions<AzureBlobStorageOptions>()
            .Bind(configuration.GetSection(AzureBlobStorageOptions.SectionName))
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
            }, "Azure Blob Storage configuration validation failed");

        return services;
    }

    /// <summary>
    /// Configures agent services
    /// </summary>
    public static IServiceCollection AddAgents(this IServiceCollection services)
    {
        services.AddScoped<ImageAnalysisAgent>();
        return services;
    }
}