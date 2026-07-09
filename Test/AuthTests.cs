using API.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

public sealed class AuthTests
{
    [Fact]
    public void DeriveUserIDFromEmail_MatchesExistingUIAlgorithm()
    {
        var userID = FolioTraceUserIdentityService.DeriveUserIDFromEmail(" PERSON@example.com ");

        Assert.Equal(Guid.Parse("32fa305b-1eb1-5fd2-9f00-61d16e627499"), userID);
    }

    [Fact]
    public void CurrentUserFromPrincipal_MapsSessionClaims()
    {
        var identity = new FolioTraceUserIdentity(
            Guid.Parse("32fa305b-1eb1-5fd2-9f00-61d16e627499"),
            "prof_123",
            "person@example.com",
            "Person Example",
            "org_123");

        var principal = AuthEndpointRegistration.CreatePrincipal(identity);
        var currentUser = AuthEndpointRegistration.CurrentUserFromPrincipal(principal);

        Assert.NotNull(currentUser);
        Assert.Equal(identity.UserID, currentUser.UserID);
        Assert.Equal(identity.WorkOSUserID, currentUser.WorkOSUserID);
        Assert.Equal(identity.Email, currentUser.Email);
        Assert.Equal(identity.DisplayName, currentUser.DisplayName);
        Assert.Equal(identity.OrganizationID, currentUser.OrganizationID);
    }

    [Fact]
    public void CreatePrincipal_IncludesWorkOSSessionClaimWhenAvailable()
    {
        var identity = new FolioTraceUserIdentity(
            Guid.Parse("32fa305b-1eb1-5fd2-9f00-61d16e627499"),
            "prof_123",
            "person@example.com",
            "Person Example",
            "org_123");

        var principal = AuthEndpointRegistration.CreatePrincipal(identity, "session_123");

        Assert.Equal("session_123", principal.FindFirst(FolioTraceAuthClaims.WorkOSSessionID)?.Value);
    }

    [Fact]
    public async Task UserConsistencyEndpointFilter_ReturnsForbidForMismatchedQueryUserID()
    {
        var currentUserID = Guid.Parse("32fa305b-1eb1-5fd2-9f00-61d16e627499");
        var otherUserID = Guid.Parse("7af18420-8cbf-4414-bdbb-5e7daff79feb");
        var httpContext = CreateAuthenticatedContext(currentUserID);
        httpContext.Request.QueryString = new QueryString($"?userID={otherUserID}");

        var result = await new UserConsistencyEndpointFilter().InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext, []),
            _ => new ValueTask<object?>("next"));

        AssertStatusCode(result, StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task UserConsistencyEndpointFilter_ReturnsForbidForMismatchedBodyUserID()
    {
        var currentUserID = Guid.Parse("32fa305b-1eb1-5fd2-9f00-61d16e627499");
        var otherUserID = Guid.Parse("7af18420-8cbf-4414-bdbb-5e7daff79feb");
        var httpContext = CreateAuthenticatedContext(currentUserID);

        var result = await new UserConsistencyEndpointFilter().InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext, [new TestUserRequest(otherUserID)]),
            _ => new ValueTask<object?>("next"));

        AssertStatusCode(result, StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task UserConsistencyEndpointFilter_AllowsMatchingUserID()
    {
        var currentUserID = Guid.Parse("32fa305b-1eb1-5fd2-9f00-61d16e627499");
        var httpContext = CreateAuthenticatedContext(currentUserID);
        httpContext.Request.QueryString = new QueryString($"?userID={currentUserID}");

        var result = await new UserConsistencyEndpointFilter().InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext, [new TestUserRequest(currentUserID)]),
            _ => new ValueTask<object?>("next"));

        Assert.Equal("next", result);
    }

    [Fact]
    public void WorkOSAuthKitClient_GetAuthorizationUrl_UsesConfiguredCallbackAndOrganization()
    {
        var client = new WorkOSAuthKitClient(
            Options.Create(new WorkOSAuthOptions
            {
                ClientId = "client_123",
                ApiKey = "sk_test_123",
                RedirectUri = "https://localhost:7058/API/Auth/Callback",
                OrganizationIds = ["org_123"]
            }));

        var authorizationUrl = client.GetAuthorizationUrl("state_123");
        var query = ParseQueryString(new Uri(authorizationUrl).Query);

        Assert.Equal("client_123", query["client_id"]);
        Assert.Equal("https://localhost:7058/API/Auth/Callback", query["redirect_uri"]);
        Assert.Equal("org_123", query["organization_id"]);
        Assert.Equal("state_123", query["state"]);
    }

    [Fact]
    public void WorkOSAuthKitClient_GetLogoutUrl_UsesSessionAndReturnUrl()
    {
        var client = new WorkOSAuthKitClient(
            Options.Create(new WorkOSAuthOptions
            {
                ClientId = "client_123",
                ApiKey = "sk_test_123",
                RedirectUri = "https://localhost:7058/API/Auth/Callback"
            }));

        var logoutUrl = client.GetLogoutUrl("session_123", "https://localhost:5173/");
        var query = ParseQueryString(new Uri(logoutUrl).Query);

        Assert.Equal("session_123", query["session_id"]);
        Assert.Equal("https://localhost:5173/", query["return_to"]);
    }

    [Fact]
    public void WorkOSAuthKitClient_GetSessionIdFromAccessToken_ReadsSidClaim()
    {
        var accessToken = $"{ToBase64Url("""{"alg":"none"}""")}.{ToBase64Url("""{"sid":"session_123"}""")}.";

        var sessionID = WorkOSAuthKitClient.GetSessionIdFromAccessToken(accessToken);

        Assert.Equal("session_123", sessionID);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(Guid userID)
    {
        var context = new DefaultHttpContext();
        context.User = AuthEndpointRegistration.CreatePrincipal(new FolioTraceUserIdentity(
            userID,
            "prof_123",
            "person@example.com",
            "Person Example",
            "org_123"));
        return context;
    }

    private static void AssertStatusCode(object? result, int expectedStatusCode)
    {
        Assert.IsAssignableFrom<IResult>(result);
        Assert.NotNull(result);
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        Assert.NotNull(statusCodeProperty);
        var statusCode = statusCodeProperty.GetValue(result);
        Assert.Equal(expectedStatusCode, statusCode);
    }

    private static Dictionary<string, string> ParseQueryString(string query) =>
        query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                pair => Uri.UnescapeDataString(pair[0]),
                pair => pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty);

    private static string ToBase64Url(string value) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed record TestUserRequest(Guid UserID);

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext, IList<object?> arguments)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = arguments;

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
