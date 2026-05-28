using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingInspecieInCreatedEventBuilder
{
    public static Result<HoldingInspecieInCreatedEvent> Create(HoldingInspecieInCreatedRequest request, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.Create<HoldingInspecieInCreatedEvent, HoldingInspecieIn>(request, CreateEvent, accounts, instruments, holdings);

    public static Result<HoldingInspecieInCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.CreateSeed<HoldingInspecieInCreatedEvent, HoldingInspecieIn>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, CreateEvent, accounts, instruments, holdings);

    private static HoldingInspecieInCreatedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault);
}
