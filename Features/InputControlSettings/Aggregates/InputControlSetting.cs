using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record InputControlSetting : IModel
{
    public required InputControlKind ControlKind { get; init; }

    public required InputControlSettingScope Scope { get; init; }

    public AccountID? AccountID { get; init; }

    public UserID? UserID { get; init; }

    public short? DecimalPlaces { get; init; }

    public decimal? MinValue { get; init; }

    public decimal? MaxValue { get; init; }

    public string? FormatPattern { get; init; }

    public bool? AllowNegative { get; init; }

    public required EventDateTime ValuationDateTime { get; init; }

    public required AuditDateTime AsOfDateTime { get; init; }

    public required EventID LastEventID { get; init; }

    public required LastAuditDateTime LastAuditDateTime { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public InputControlSetting(
        InputControlKind controlKind,
        InputControlSettingScope scope,
        AccountID? accountID,
        UserID? userID,
        short? decimalPlaces,
        decimal? minValue,
        decimal? maxValue,
        string? formatPattern,
        bool? allowNegative,
        EventDateTime valuationDateTime,
        AuditDateTime asOfDateTime,
        EventID lastEventID,
        LastAuditDateTime lastAuditDateTime)
    {
        ControlKind = controlKind;
        Scope = scope;
        AccountID = accountID;
        UserID = userID;
        DecimalPlaces = decimalPlaces;
        MinValue = minValue;
        MaxValue = maxValue;
        FormatPattern = string.IsNullOrWhiteSpace(formatPattern) ? null : formatPattern.Trim();
        AllowNegative = allowNegative;
        ValuationDateTime = valuationDateTime;
        AsOfDateTime = asOfDateTime;
        LastEventID = lastEventID;
        LastAuditDateTime = lastAuditDateTime;
    }

    [SetsRequiredMembers]
    public InputControlSetting(InputControlSettingDefinition definition, IEventBase source)
        : this(
            definition.ControlKind,
            definition.Scope,
            definition.AccountID,
            definition.UserID,
            definition.DecimalPlaces,
            definition.MinValue,
            definition.MaxValue,
            definition.FormatPattern,
            definition.AllowNegative,
            source.EventDateTime,
            source.AuditDateTime,
            source.EventID,
            new LastAuditDateTime(source.AuditDateTime.Value))
    {
    }

    public string ScopeKey => InputControlSettingKeys.ScopeKey(Scope, AccountID, UserID);
}
