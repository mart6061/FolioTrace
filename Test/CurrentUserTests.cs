using API;
using FolioTrace;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Test;

public sealed class CurrentUserTests
{
    private readonly FixedCurrentUserContext currentUserContext = new();

    [Fact]
    public void FixedCurrentUserContext_UsesTheInitialisationUser()
    {
        var currentUser = currentUserContext.Current;

        Assert.Equal(Constants.Initialisation.UserID.Value, currentUser.UserID);
        Assert.Equal("local@foliotrace.invalid", currentUser.Email);
        Assert.Equal("FolioTrace Local User", currentUser.DisplayName);
    }

    [Fact]
    public void CurrentUserResponse_UsesTheProviderNeutralShape()
    {
        var response = currentUserContext.Current.ToResponse();
        var json = JsonSerializer.Serialize(response);

        Assert.Equal(currentUserContext.Current.UserID, response.UserID);
        Assert.Equal(currentUserContext.Current.Email, response.Email);
        Assert.Equal(currentUserContext.Current.DisplayName, response.DisplayName);
        Assert.Equal(
            """{"userID":"334f6bb3-762d-4d10-9752-f913d75f7c6c","email":"local@foliotrace.invalid","displayName":"FolioTrace Local User"}""",
            json);
    }

    [Fact]
    public async Task UserConsistencyEndpointFilter_ReturnsForbidForMismatchedQueryUserID()
    {
        var otherUserID = Guid.Parse("7af18420-8cbf-4414-bdbb-5e7daff79feb");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?userID={otherUserID}");

        var result = await CreateFilter().InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext, []),
            _ => new ValueTask<object?>("next"));

        AssertStatusCode(result, StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task UserConsistencyEndpointFilter_ReturnsForbidForMismatchedBodyUserID()
    {
        var otherUserID = Guid.Parse("7af18420-8cbf-4414-bdbb-5e7daff79feb");
        var httpContext = new DefaultHttpContext();

        var result = await CreateFilter().InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext, [new TestUserRequest(otherUserID)]),
            _ => new ValueTask<object?>("next"));

        AssertStatusCode(result, StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task UserConsistencyEndpointFilter_AllowsMatchingUserID()
    {
        var currentUserID = currentUserContext.Current.UserID;
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString($"?userID={currentUserID}");

        var result = await CreateFilter().InvokeAsync(
            new TestEndpointFilterInvocationContext(httpContext, [new TestUserRequest(currentUserID)]),
            _ => new ValueTask<object?>("next"));

        Assert.Equal("next", result);
    }

    [Fact]
    public async Task UserConsistencyEndpointFilter_AllowsRequestWithoutUserID()
    {
        var result = await CreateFilter().InvokeAsync(
            new TestEndpointFilterInvocationContext(new DefaultHttpContext(), [new object()]),
            _ => new ValueTask<object?>("next"));

        Assert.Equal("next", result);
    }

    private UserConsistencyEndpointFilter CreateFilter() => new(currentUserContext);

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

    private sealed class TestEndpointFilterInvocationContext(HttpContext httpContext, IList<object?> arguments)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;

        public override IList<object?> Arguments { get; } = arguments;

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
