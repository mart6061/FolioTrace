namespace API.Auth;

public sealed class WorkOSAuthOptions
{
    public const string SectionName = "WorkOS";

    /// <summary>
    /// WorkOS Client ID (starts with client_).
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// WorkOS API Key (starts with sk_).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// OAuth callback URI registered in WorkOS dashboard.
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the frontend application for post-auth redirects.
    /// </summary>
    public string ApplicationBaseUrl { get; set; } = "/";

    /// <summary>
    /// Optional: Restrict authentication to specific WorkOS organization IDs.
    /// </summary>
    public List<string> OrganizationIds { get; set; } = [];

    /// <summary>
    /// Cookie name for the session authentication cookie.
    /// </summary>
    public string CookieName { get; set; } = "FolioTrace.Session";

    /// <summary>
    /// Cookie name for the CSRF state during OAuth flow.
    /// </summary>
    public string StateCookieName { get; set; } = "FolioTrace.WorkOS.State";

    /// <summary>
    /// Session lifetime in minutes.
    /// </summary>
    public int SessionLifetimeMinutes { get; set; } = 480;

    /// <summary>
    /// Returns true if the minimum required configuration is present for AuthKit.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(RedirectUri);
}
