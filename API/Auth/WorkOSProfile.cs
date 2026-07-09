using System.Text.Json.Serialization;

namespace API.Auth;

public sealed record WorkOSProfile(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    [property: JsonPropertyName("organization_id")] string? OrganizationId);

public sealed record WorkOSProfileAndToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("profile")] WorkOSProfile Profile,
    [property: JsonPropertyName("session_id")] string? SessionId);
