using System.Reflection;

namespace API.Auth;

public sealed class UserConsistencyEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var currentUser = AuthEndpointRegistration.CurrentUserFromPrincipal(context.HttpContext.User);
        if (currentUser is null)
            return Results.StatusCode(StatusCodes.Status401Unauthorized);

        if (!QueryUserMatches(context.HttpContext.Request.Query, currentUser.UserID))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        foreach (var argument in context.Arguments)
        {
            if (argument is null)
                continue;

            if (!ArgumentUserMatches(argument, currentUser.UserID))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }

    private static bool QueryUserMatches(IQueryCollection query, Guid currentUserID)
    {
        if (!query.TryGetValue("userID", out var values))
            return true;

        return values.Count == 0
            || string.IsNullOrWhiteSpace(values[0])
            || Guid.TryParse(values[0], out var userID) && userID == currentUserID;
    }

    private static bool ArgumentUserMatches(object argument, Guid currentUserID)
    {
        var property = argument.GetType().GetProperty("UserID", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
            return true;

        var value = property.GetValue(argument);
        return value switch
        {
            null => true,
            Guid userID => userID == currentUserID,
            string userID => Guid.TryParse(userID, out var parsedUserID) && parsedUserID == currentUserID,
            _ when value.ToString() is { } text => Guid.TryParse(text, out var parsedUserID) && parsedUserID == currentUserID,
            _ => false
        };
    }
}
