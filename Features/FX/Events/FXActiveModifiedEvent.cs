using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FXActiveModifiedEvent : EventBase, IFXEvent
{
    public CurrencyPair Pair { get; init; } = null!;

    public bool Active { get; init; }

    [JsonConstructor]
    private FXActiveModifiedEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal FXActiveModifiedEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, CurrencyPair pair, bool active)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        Pair = pair;
        Active = active;
    }

    public override string Type => nameof(FXActiveModifiedEvent);

    public override string ToData() => $"{base.ToData()}|{Pair.ToData()}|{Active}";

    public override string ToDetail() => $"{nameof(FXActiveModifiedEvent)}: ({base.ToDetail()}, Pair: {Pair.ToDetail()}, Active: {Active})";

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, CurrencyPair? pair)
    {
        var messages = FXCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair).ToList();
        return messages;
    }
}
