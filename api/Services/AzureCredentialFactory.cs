using Azure.Identity;
using AgentHub.Api.Models;
using Microsoft.Extensions.Options;

namespace AgentHub.Api.Services;

/// <summary>
/// Factory for creating Azure credentials with consistent configuration
/// </summary>
public interface IAzureCredentialFactory
{
    /// <summary>
    /// Creates a DefaultAzureCredential with tenant-aware configuration
    /// </summary>
    /// <returns>Configured DefaultAzureCredential</returns>
    DefaultAzureCredential CreateDefaultAzureCredential();
    
    /// <summary>
    /// Diagnoses available authentication methods for troubleshooting
    /// </summary>
    /// <returns>Diagnostic information about authentication methods</returns>
    string DiagnoseAuthentication();
}

/// <summary>
/// Implementation of Azure credential factory with tenant-aware configuration
/// </summary>
public class AzureCredentialFactory : IAzureCredentialFactory
{
    private readonly AzureCredentialOptions _options;
    private readonly ILogger<AzureCredentialFactory> _logger;

    public AzureCredentialFactory(IOptions<AzureCredentialOptions> options, ILogger<AzureCredentialFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a DefaultAzureCredential with tenant-aware configuration
    /// </summary>
    /// <returns>Configured DefaultAzureCredential</returns>
    public DefaultAzureCredential CreateDefaultAzureCredential()
    {
        try
        {
            var credentialOptions = new DefaultAzureCredentialOptions();
            
            // Configure Visual Studio tenant ID if specified
            if (_options.HasVisualStudioTenantId)
            {
                _logger.LogDebug("Creating DefaultAzureCredential with VisualStudioTenantId: {TenantId}", _options.VisualStudioTenantId);
                credentialOptions.VisualStudioTenantId = _options.VisualStudioTenantId;
            }
            else
            {
                _logger.LogDebug("Creating DefaultAzureCredential with default options (no VisualStudioTenantId specified)");
            }
            
            // Configure Managed Identity client ID if specified
            if (_options.HasManagedIdentityClientId)
            {
                _logger.LogDebug("Creating DefaultAzureCredential with ManagedIdentityClientId: {ClientId}", _options.ManagedIdentityClientId);
                credentialOptions.ManagedIdentityClientId = _options.ManagedIdentityClientId;
            }
            
            // Configure credential exclusions
            credentialOptions.ExcludeVisualStudioCredential = _options.ExcludeVisualStudioCredential;
            credentialOptions.ExcludeAzureCliCredential = _options.ExcludeAzureCliCredential;
            credentialOptions.ExcludeEnvironmentCredential = _options.ExcludeEnvironmentCredential;
            credentialOptions.ExcludeManagedIdentityCredential = _options.ExcludeManagedIdentityCredential;
            credentialOptions.ExcludeAzurePowerShellCredential = _options.ExcludeAzurePowerShellCredential;
            credentialOptions.ExcludeInteractiveBrowserCredential = _options.ExcludeInteractiveBrowserCredential;
            
            // Add diagnostic information
            var diagnostics = DiagnoseAuthentication();
            _logger.LogDebug("Authentication diagnostics: {Diagnostics}", diagnostics);
            
            return new DefaultAzureCredential(credentialOptions);
        }
        catch (Exception ex)
        {
            var diagnostics = DiagnoseAuthentication();
            _logger.LogError(ex, "Failed to create DefaultAzureCredential. Diagnostics: {Diagnostics}", diagnostics);
            
            _logger.LogError("Troubleshooting: Run 'az login', ensure Visual Studio is signed in, check RBAC permissions");
            throw;
        }
    }
    
    /// <summary>
    /// Diagnoses available authentication methods for troubleshooting
    /// </summary>
    /// <returns>Diagnostic information about authentication methods</returns>
    public string DiagnoseAuthentication()
    {
        var diagnostics = new List<string>();
        
        // Check environment variables
        var hasClientId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID"));
        var hasClientSecret = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET"));
        var hasTenantId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_TENANT_ID"));
        
        if (hasClientId && hasClientSecret && hasTenantId)
        {
            diagnostics.Add("Environment variables available");
        }
        else
        {
            diagnostics.Add("No environment variables");
        }
        
        // Check Visual Studio tenant configuration
        if (_options.HasVisualStudioTenantId)
        {
            diagnostics.Add($"Visual Studio tenant configured: {_options.VisualStudioTenantId}");
        }
        else
        {
            diagnostics.Add("No Visual Studio tenant specified");
        }
        
        // Check Managed Identity configuration
        var isManagedIdentityEnvironment = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT")) ||
                                          !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSI_ENDPOINT"));
        
        if (isManagedIdentityEnvironment)
        {
            diagnostics.Add("Managed Identity environment detected");
        }
        else
        {
            diagnostics.Add("Not in managed identity environment");
        }
        
        return string.Join("; ", diagnostics);
    }
}