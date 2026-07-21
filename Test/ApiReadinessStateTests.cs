using API;
using Microsoft.AspNetCore.Http;

namespace Test;

public sealed class ApiReadinessStateTests
{
    [Fact]
    public async Task MarkNotReady_ResetsReadinessUntilMarkedReadyAgain()
    {
        var state = new ApiReadinessState();
        state.MarkReady();

        state.MarkNotReady();
        var waitForReady = state.WaitUntilReadyAsync(CancellationToken.None);

        Assert.False(state.Ready);
        Assert.False(waitForReady.IsCompleted);

        state.MarkReady();

        await waitForReady;
        Assert.True(state.Ready);
    }

    [Theory]
    [InlineData("/System/Health")]
    [InlineData("/System/Build")]
    [InlineData("/Notifications/Aggregates")]
    public async Task Middleware_AllowsBuildSupportEndpointsWhileNotReady(string path)
    {
        var nextCalled = false;
        var middleware = new ApiReadinessMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context, new ApiReadinessState());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Middleware_RejectsNormalApiCallsWhileNotReady()
    {
        var middleware = new ApiReadinessMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/Countries";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, new ApiReadinessState());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }
}
