using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record User : IModel
{
    public required UserID UserID { get; init; }

    public required string DisplayName { get; init; }

    public required UserDisplayPreferences DisplayPreferences { get; init; }

    public required UserProfileValuationPreferences ValuationPreferences { get; init; }

    public required EventDateTime? LastSignedIn { get; init; }

    public required EventDateTime? LastSignedOut { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public User(
        UserID userID,
        string displayName,
        UserDisplayPreferences displayPreferences,
        UserProfileValuationPreferences valuationPreferences,
        EventDateTime? lastSignedIn,
        EventDateTime? lastSignedOut,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        EventID lastEventID,
        LastAuditDateTime lastAuditDateTime)
    {
        UserID = userID;
        DisplayName = displayName;
        DisplayPreferences = displayPreferences;
        ValuationPreferences = valuationPreferences;
        LastSignedIn = lastSignedIn;
        LastSignedOut = lastSignedOut;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    public override string ToString() => DisplayName;
}
