using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace API.Auth;

public static class AuthEndpointRegistration
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/Auth").WithTags("Auth");

        auth.MapGet("/SSO", (
            HttpContext context,
            IWorkOSAuthKitClient workOSClient,
            WorkOSAuthorizationStateService stateService,
            IOptions<WorkOSAuthOptions> options,
            string? returnTo) =>
        {
            if (!options.Value.IsConfigured)
                return Results.Redirect(GetApplicationRedirectUrl("/auth/error?code=AUTH_NOT_CONFIGURED", options.Value));

            var state = stateService.Create(context.Response, returnTo);
            return Results.Redirect(workOSClient.GetAuthorizationUrl(state.State));
        }).AllowAnonymous();

        auth.MapGet("/Callback", async (
            HttpContext context,
            IWorkOSAuthKitClient workOSClient,
            WorkOSAuthorizationStateService stateService,
            FolioTraceUserIdentityService identityService,
            IOptions<WorkOSAuthOptions> options,
            string? code,
            string? state,
            string? error,
            CancellationToken cancellationToken) =>
        {
            if (!string.IsNullOrWhiteSpace(error))
                return Results.Redirect(GetApplicationRedirectUrl("/auth/error?code=AUTH_ERROR", options.Value));

            if (string.IsNullOrWhiteSpace(code))
                return Results.Redirect(GetApplicationRedirectUrl("/auth/error?code=AUTH_ERROR", options.Value));

            var authorizationState = stateService.Validate(context.Request, context.Response, state);
            if (authorizationState is null)
                return Results.Redirect(GetApplicationRedirectUrl("/auth/error?code=STATE_INVALID", options.Value));

            try
            {
                var profileAndToken = await workOSClient.AuthenticateWithCodeAsync(code, cancellationToken);
                if (!IsAllowedOrganization(profileAndToken.Profile.OrganizationId, options.Value))
                    return Results.Redirect(GetApplicationRedirectUrl("/auth/error?code=ORGANIZATION_NOT_ALLOWED", options.Value));

                var identity = identityService.CreateIdentity(profileAndToken.Profile);
                await identityService.EnsureUserAsync(identity, cancellationToken);
                await identityService.RecordSignInAsync(identity, cancellationToken);
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    CreatePrincipal(identity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Max(5, options.Value.SessionLifetimeMinutes)),
                        AllowRefresh = true
                    });

                return Results.Redirect(GetApplicationRedirectUrl(authorizationState.ReturnTo, options.Value));
            }
            catch (Exception exception)
            {
                context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("FolioTrace.Auth")
                    .LogError(exception, "WorkOS AuthKit callback failed.");
                return Results.Redirect(GetApplicationRedirectUrl("/auth/error?code=AUTH_ERROR", options.Value));
            }
        }).AllowAnonymous();

        auth.MapGet("/Session", (ClaimsPrincipal user) =>
        {
            var currentUser = CurrentUserFromPrincipal(user);
            return currentUser is null ? Results.Unauthorized() : Results.Ok(currentUser.ToResponse());
        }).RequireAuthorization();

        auth.MapGet("/SignOut", async (
            HttpContext context,
            FolioTraceUserIdentityService identityService,
            string? returnTo,
            CancellationToken cancellationToken) =>
        {
            var currentUser = CurrentUserFromPrincipal(context.User);
            if (currentUser is not null)
                await identityService.RecordSignOutAsync(currentUser.UserID, cancellationToken);

            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect(GetApplicationRedirectUrl(WorkOSAuthorizationStateService.NormalizeReturnTo(returnTo), context.RequestServices.GetRequiredService<IOptions<WorkOSAuthOptions>>().Value));
        }).RequireAuthorization();

        return api;
    }

    public static ClaimsPrincipal CreatePrincipal(FolioTraceUserIdentity identity)
    {
        var claims = new[]
        {
            new Claim(FolioTraceAuthClaims.UserID, identity.UserID.ToString()),
            new Claim(FolioTraceAuthClaims.WorkOSUserID, identity.WorkOSUserID),
            new Claim(FolioTraceAuthClaims.Email, identity.Email),
            new Claim(FolioTraceAuthClaims.DisplayName, identity.DisplayName),
            new Claim(FolioTraceAuthClaims.OrganizationID, identity.OrganizationID),
            new Claim(ClaimTypes.NameIdentifier, identity.UserID.ToString()),
            new Claim(ClaimTypes.Email, identity.Email),
            new Claim(ClaimTypes.Name, identity.DisplayName)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static FolioTraceUserIdentity? CurrentUserFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return null;

        var userIDValue = principal.FindFirstValue(FolioTraceAuthClaims.UserID);
        var workOSUserID = principal.FindFirstValue(FolioTraceAuthClaims.WorkOSUserID);
        var email = principal.FindFirstValue(FolioTraceAuthClaims.Email);
        var displayName = principal.FindFirstValue(FolioTraceAuthClaims.DisplayName);
        var organizationID = principal.FindFirstValue(FolioTraceAuthClaims.OrganizationID);

        return Guid.TryParse(userIDValue, out var userID)
            && !string.IsNullOrWhiteSpace(workOSUserID)
            && !string.IsNullOrWhiteSpace(email)
            && !string.IsNullOrWhiteSpace(displayName)
            && !string.IsNullOrWhiteSpace(organizationID)
                ? new FolioTraceUserIdentity(userID, workOSUserID, email, displayName, organizationID)
                : null;
    }

    private static bool IsAllowedOrganization(string? organizationID, WorkOSAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(organizationID))
            return false;

        return options.OrganizationIds.Count == 0
            || options.OrganizationIds.Any(allowed => string.Equals(allowed, organizationID, StringComparison.Ordinal));
    }

    private static string GetApplicationRedirectUrl(string returnTo, WorkOSAuthOptions options)
    {
        var normalizedReturnTo = WorkOSAuthorizationStateService.NormalizeReturnTo(returnTo);
        if (Uri.TryCreate(options.ApplicationBaseUrl, UriKind.Absolute, out var applicationBaseUrl))
            return new Uri(applicationBaseUrl, normalizedReturnTo).ToString();

        return normalizedReturnTo;
    }
}
