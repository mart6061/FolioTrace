using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingInspecieOutCreatedEventBuilder
{
    public static Result<HoldingInspecieOutCreatedEvent> Create(HoldingInspecieOutCreatedRequest request, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.Create<HoldingInspecieOutCreatedEvent, HoldingInspecieOut>(request, CreateEvent, accounts, instruments, holdings);

    public static Result<HoldingInspecieOutCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.CreateSeed<HoldingInspecieOutCreatedEvent, HoldingInspecieOut>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, CreateEvent, accounts, instruments, holdings);

    private static HoldingInspecieOutCreatedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault);
}
