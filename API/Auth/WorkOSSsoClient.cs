using Microsoft.Extensions.Options;
using WorkOS;

namespace API.Auth;

/// <summary>
/// WorkOS AuthKit client interface for authentication operations.
/// </summary>
public interface IWorkOSAuthKitClient
{
    /// <summary>
    /// Generates an AuthKit authorization URL for redirecting users.
    /// </summary>
    string GetAuthorizationUrl(string state);

    /// <summary>
    /// Exchanges an authorization code for user profile and access token.
    /// </summary>
    Task<WorkOSProfileAndToken> AuthenticateWithCodeAsync(string code, CancellationToken cancellationToken);
}

/// <summary>
/// WorkOS AuthKit client using the official WorkOS.net SDK.
/// </summary>
public sealed class WorkOSAuthKitClient : IWorkOSAuthKitClient
{
    private readonly WorkOSAuthOptions _options;

    public WorkOSAuthKitClient(IOptions<WorkOSAuthOptions> options)
    {
        _options = options.Value;

        // Configure the global WorkOS client with API key and Client ID
        var client = new WorkOSClient(new WorkOSOptions
        {
            ApiKey = _options.ApiKey,
            ClientId = _options.ClientId
        });
        WorkOSConfiguration.WorkOSClient = client;
    }

    public string GetAuthorizationUrl(string state)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("WorkOS AuthKit is not configured.");

        // Use the SDK's UserManagement to generate AuthKit authorization URL
        return WorkOSConfiguration.WorkOSClient.UserManagement.GetAuthorizationUrl(
            new UserManagementGetAuthorizationUrlOptions
            {
                RedirectUri = _options.RedirectUri,
                Provider = UserManagementAuthenticationProvider.Authkit,
                State = state
            });
    }

    public async Task<WorkOSProfileAndToken> AuthenticateWithCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("WorkOS AuthKit is not configured.");

        // Exchange authorization code for user profile using the SDK
        var authResponse = await WorkOSConfiguration.WorkOSClient.UserManagement
            .AuthenticateWithCodeAsync(new AuthenticateWithCodeOptions
            {
                Code = code
            });

        // Map SDK response to our internal profile format
        var profile = new WorkOSProfile(
            Id: authResponse.User.Id,
            Email: authResponse.User.Email,
            FirstName: authResponse.User.FirstName,
            LastName: authResponse.User.LastName,
            OrganizationId: authResponse.OrganizationId);

        return new WorkOSProfileAndToken(
            AccessToken: authResponse.AccessToken,
            Profile: profile);
    }
}

// Keep legacy interface as alias for backwards compatibility
public interface IWorkOSSsoClient : IWorkOSAuthKitClient { }
