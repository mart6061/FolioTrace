using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace API.Auth;

public sealed class WorkOSAuthorizationStateService(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<WorkOSAuthOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("FolioTrace.WorkOS.AuthorizationState.v1");
    private readonly WorkOSAuthOptions options = options.Value;

    public WorkOSAuthorizationState Create(HttpResponse response, string? returnTo)
    {
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var authorizationState = new WorkOSAuthorizationState(state, NormalizeReturnTo(returnTo));
        var value = protector.Protect(JsonSerializer.Serialize(authorizationState, JsonOptions));

        response.Cookies.Append(options.StateCookieName, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(10)
        });

        return authorizationState;
    }

    public WorkOSAuthorizationState? Validate(HttpRequest request, HttpResponse response, string? returnedState)
    {
        response.Cookies.Delete(options.StateCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        if (string.IsNullOrWhiteSpace(returnedState))
            return null;

        if (!request.Cookies.TryGetValue(options.StateCookieName, out var protectedValue) || string.IsNullOrWhiteSpace(protectedValue))
            return null;

        try
        {
            var json = protector.Unprotect(protectedValue);
            var authorizationState = JsonSerializer.Deserialize<WorkOSAuthorizationState>(json, JsonOptions);
            return authorizationState?.State == returnedState ? authorizationState : null;
        }
        catch
        {
            return null;
        }
    }

    public static string NormalizeReturnTo(string? returnTo)
    {
        if (string.IsNullOrWhiteSpace(returnTo))
            return "/";

        if (!returnTo.StartsWith("/", StringComparison.Ordinal) || returnTo.StartsWith("//", StringComparison.Ordinal))
            return "/";

        if (returnTo.StartsWith("/API/Auth", StringComparison.OrdinalIgnoreCase))
            return "/";

        return returnTo;
    }
}
