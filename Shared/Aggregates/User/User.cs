using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record User : IModel
{
    public required UserID UserID { get; init; }

    public required string DisplayName { get; init; }

    public required UserDisplayPreferences DisplayPreferences { get; init; }

    public required UserValuationPreferences ValuationPreferences { get; init; }

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
        UserValuationPreferences valuationPreferences,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        EventID lastEventID,
        LastAuditDateTime lastAuditDateTime)
    {
        UserID = userID;
        DisplayName = displayName;
        DisplayPreferences = displayPreferences;
        ValuationPreferences = valuationPreferences;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    public override string ToString() => DisplayName;

    public string ToData() => $"{UserID.ToData()}|{DisplayName}|{DisplayPreferences.ToData()}|{ValuationPreferences.ToData()}|{ValuationDateTime.ToData()}|{AsOfDateTime.ToData()}|{LastEventID.ToData()}|{LastAuditDateTime.ToData()}";

    public string ToDetail() => $"{nameof(User)}: (UserID: {UserID.ToDetail()}, DisplayName: {DisplayName}, DisplayPreferences: {DisplayPreferences.ToDetail()}, ValuationPreferences: {ValuationPreferences.ToDetail()}, ValuationDateTime: {ValuationDateTime.ToDetail()}, AsOfDateTime: {AsOfDateTime.ToDetail()}, LastEventID: {LastEventID.ToDetail()}, LastAuditDateTime: {LastAuditDateTime.ToDetail()})";
}
