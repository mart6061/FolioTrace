using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class BrokerTradeMethodEventBuilder
{
    public static Result<IBrokerEvent> Set(BrokerTradeMethodSetRequest request) =>
        SetSeed(Guid.CreateGuid7(), request.UserID, request.EventDateTime, AuditDateTimeBuilder.Create(), request.Reason, request.LEI, request.TradeMethod);

    public static Result<IBrokerEvent> SetSeed(EventID eventID, UserID userID, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, LegalEntityIdentifier lei, ITradeMethod tradeMethod)
    {
        var errors = BrokerEventValidation.ValidateBase(eventID, userID, eventDateTime, auditDateTime, reason, lei).ToList();
        ValidateTradeMethod(tradeMethod, errors);
        if (errors.Count > 0)
            return Result<IBrokerEvent>.Invalid(errors);

        IBrokerEvent @event = tradeMethod switch
        {
            FIXTradeMethod method => new BrokerFIXTradeMethodSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, lei, method),
            PhoneTradeMethod method => new BrokerPhoneTradeMethodSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, lei, method),
            FaxTradeMethod method => new BrokerFaxTradeMethodSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, lei, method),
            TradeFileTradeMethod method => new BrokerTradeFileTradeMethodSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, lei, method),
            ManualTradeMethod method => new BrokerManualTradeMethodSetEvent(eventID, userID, eventDateTime, auditDateTime, reason, lei, method),
            _ => throw new InvalidOperationException($"Unsupported trade method '{tradeMethod.GetType().Name}'.")
        };
        return Result<IBrokerEvent>.Success(@event);
    }

    public static Result<BrokerTradeMethodUnsetEvent> Unset(BrokerTradeMethodUnsetRequest request)
    {
        var eventID = new EventID(Guid.CreateGuid7());
        var auditDateTime = AuditDateTimeBuilder.Create();
        var errors = BrokerEventValidation.ValidateBase(eventID, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.LEI).ToList();
        return errors.Count == 0
            ? Result<BrokerTradeMethodUnsetEvent>.Success(new BrokerTradeMethodUnsetEvent(eventID, request.UserID, request.EventDateTime, auditDateTime, request.Reason, request.LEI, request.TradeMethodType))
            : Result<BrokerTradeMethodUnsetEvent>.Invalid(errors);
    }

    private static void ValidateTradeMethod(ITradeMethod? tradeMethod, List<string> errors)
    {
        if (tradeMethod is null) { errors.Add("TradeMethod is required."); return; }
        if (tradeMethod is FIXTradeMethod fix && (string.IsNullOrWhiteSpace(fix.Host) || fix.Port is < 1 or > 65535 || string.IsNullOrWhiteSpace(fix.SenderCompID) || string.IsNullOrWhiteSpace(fix.TargetCompID) || fix.HeartbeatSeconds < 1))
            errors.Add("FIX trade method connection settings are invalid.");
        if (tradeMethod is TradeFileTradeMethod file && (file.Columns.Count == 0 || file.Columns.Distinct().Count() != file.Columns.Count || file.SendConfig is null))
            errors.Add("TradeFile method requires unique columns and a send configuration.");
    }
}
