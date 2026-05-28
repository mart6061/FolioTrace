using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingFeesBankCreatedEventBuilder
{
    public static Result<HoldingFeesBankCreatedEvent> Create(HoldingFeesBankCreatedRequest request, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.Create<HoldingFeesBankCreatedEvent, HoldingFeesBank>(request, CreateEvent, accounts, instruments, holdings);

    public static Result<HoldingFeesBankCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.CreateSeed<HoldingFeesBankCreatedEvent, HoldingFeesBank>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, CreateEvent, accounts, instruments, holdings);

    private static HoldingFeesBankCreatedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault);
}
