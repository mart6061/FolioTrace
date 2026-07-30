using System.Text.Json.Serialization;
using FolioTrace;

namespace API;

public sealed record CurrentUser(
    Guid UserID,
    string Email,
    string DisplayName)
{
    public CurrentUserResponse ToResponse() => new(UserID, Email, DisplayName);
}

public sealed record CurrentUserResponse(
    [property: JsonPropertyName("userID")] Guid UserID,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("displayName")] string DisplayName);

public interface ICurrentUserContext
{
    CurrentUser Current { get; }
}

public sealed class FixedCurrentUserContext : ICurrentUserContext
{
    public CurrentUser Current { get; } = new(
        Constants.Initialisation.UserID.Value,
        "local@foliotrace.invalid",
        "FolioTrace Local User");
}
