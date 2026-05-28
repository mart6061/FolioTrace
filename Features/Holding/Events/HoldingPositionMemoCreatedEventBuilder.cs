using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingPositionMemoCreatedEventBuilder
{
    public static Result<HoldingPositionMemoCreatedEvent> Create(HoldingPositionMemoCreatedRequest request, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.Create<HoldingPositionMemoCreatedEvent, HoldingPositionMemo>(request, CreateEvent, accounts, instruments, holdings);

    public static Result<HoldingPositionMemoCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.CreateSeed<HoldingPositionMemoCreatedEvent, HoldingPositionMemo>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, CreateEvent, accounts, instruments, holdings);

    private static HoldingPositionMemoCreatedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault);
}
