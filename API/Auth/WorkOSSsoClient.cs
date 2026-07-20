using System.Text.Json;
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

    /// <summary>
    /// Generates a WorkOS logout URL for ending the hosted AuthKit session.
    /// </summary>
    string GetLogoutUrl(string sessionId, string returnTo);
}

/// <summary>
/// WorkOS AuthKit client using the official WorkOS.net SDK.
/// </summary>
public sealed class WorkOSAuthKitClient : IWorkOSAuthKitClient, IWorkOSSsoClient
{
    private readonly WorkOSAuthOptions _options;
    private readonly WorkOSClient client;

    public WorkOSAuthKitClient(IOptions<WorkOSAuthOptions> options, WorkOSClient client)
    {
        _options = options.Value;
        this.client = client;
    }

    public string GetAuthorizationUrl(string state)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("WorkOS AuthKit is not configured.");

        // Use the SDK's UserManagement to generate AuthKit authorization URL
        return client.UserManagement.GetAuthorizationUrl(
            new UserManagementGetAuthorizationUrlOptions
            {
                RedirectUri = _options.RedirectUri,
                Provider = UserManagementAuthenticationProvider.Authkit,
                OrganizationId = GetSingleAllowedOrganizationId(),
                State = state
            });
    }

    public string GetLogoutUrl(string sessionId, string returnTo)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("WorkOS AuthKit is not configured.");

        return client.UserManagement.GetLogoutUrl(
            new UserManagementGetLogoutUrlOptions
            {
                SessionId = sessionId,
                ReturnTo = returnTo
            });
    }

    private string? GetSingleAllowedOrganizationId()
    {
        var organizationIds = _options.OrganizationIds
            .Where(organizationId => !string.IsNullOrWhiteSpace(organizationId))
            .Select(organizationId => organizationId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return organizationIds.Length == 1 ? organizationIds[0] : null;
    }

    public async Task<WorkOSProfileAndToken> AuthenticateWithCodeAsync(string code, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException("WorkOS AuthKit is not configured.");

        // Exchange authorization code for user profile using the SDK
        var authResponse = await client.UserManagement
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
            Profile: profile,
            SessionId: GetSessionIdFromAccessToken(authResponse.AccessToken));
    }

    public static string? GetSessionIdFromAccessToken(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(payload);
            using var document = JsonDocument.Parse(bytes);
            return document.RootElement.TryGetProperty("sid", out var sessionId)
                ? sessionId.GetString()
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

// Keep legacy interface as alias for backwards compatibility
public interface IWorkOSSsoClient : IWorkOSAuthKitClient { }
