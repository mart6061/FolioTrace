using System.Text.Json.Serialization;

namespace API.Auth;

public sealed record CurrentUserResponse(
    [property: JsonPropertyName("userID")] Guid UserID,
    [property: JsonPropertyName("workosUserID")] string WorkOSUserID,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("displayName")] string DisplayName);
