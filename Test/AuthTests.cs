using API.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;

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
    public async Task WorkOSSsoClient_TokenExchangeSendsClientSecretInFormBody()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new TestHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "access_token": "access_123",
                  "profile": {
                    "id": "prof_123",
                    "email": "person@example.com",
                    "first_name": "Person",
                    "last_name": "Example",
                    "organization_id": "org_123"
                  }
                }
                """)
            };
        });
        var client = new WorkOSSsoClient(
            new HttpClient(handler),
            Options.Create(new WorkOSAuthOptions
            {
                ClientId = "client_123",
                ApiKey = "secret_123",
                RedirectUri = "https://localhost:7058/API/Auth/Callback",
                OrganizationIds = ["org_123"],
                TokenEndpoint = "https://workos.test/sso/token"
            }));

        var result = await client.GetProfileAndTokenAsync("code_123", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Null(capturedRequest.Headers.Authorization);
        Assert.Contains("client_id=client_123", capturedBody);
        Assert.Contains("client_secret=secret_123", capturedBody);
        Assert.Contains("code=code_123", capturedBody);
        Assert.Equal("prof_123", result.Profile.Id);
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

    private sealed record TestUserRequest(Guid UserID);

    private sealed class TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext, IList<object?> arguments)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = arguments;

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
