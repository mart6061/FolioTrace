using System.Text.Json.Serialization;
using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record FXRateSetEvent : EventBase, IFXRateEvent
{
    public CurrencyPair Pair { get; init; } = null!;

    public FXPrice Price { get; init; } = null!;

    [JsonConstructor]
    private FXRateSetEvent()
        : base(null!, null!, null!, null!, string.Empty)
    {
    }

    internal FXRateSetEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, CurrencyPair pair, FXPrice price)
        : base(eventId, userId, eventDateTime, auditDateTime, reason)
    {
        Pair = pair;
        Price = price;
    }

    public override string Type => nameof(FXRateSetEvent);

    public static IReadOnlyList<string> Validate(EventID? eventId, UserID? userId, EventDateTime? eventDateTime, AuditDateTime? auditDateTime, string? reason, CurrencyPair? pair, FXPrice? price)
    {
        var messages = FXCreatedEvent.Validate(eventId, userId, eventDateTime, auditDateTime, reason, pair).ToList();

        if (price is null)
            messages.Add("Price is required.");

        return messages;
    }
}
