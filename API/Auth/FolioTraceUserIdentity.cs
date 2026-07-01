namespace API.Auth;

public sealed record FolioTraceUserIdentity(
    Guid UserID,
    string WorkOSUserID,
    string Email,
    string DisplayName,
    string OrganizationID)
{
    public CurrentUserResponse ToResponse() => new(UserID, WorkOSUserID, Email, DisplayName);
}
