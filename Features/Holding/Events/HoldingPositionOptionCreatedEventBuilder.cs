using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class HoldingPositionOptionCreatedEventBuilder
{
    public static Result<HoldingPositionOptionCreatedEvent> Create(HoldingPositionOptionCreatedRequest request, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.Create<HoldingPositionOptionCreatedEvent, HoldingPositionOption>(request, CreateEvent, accounts, instruments, holdings);

    public static Result<HoldingPositionOptionCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault, Accounts? accounts = null, Instruments? instruments = null, Holdings? holdings = null) =>
        HoldingCreatedEventBuilderCore.CreateSeed<HoldingPositionOptionCreatedEvent, HoldingPositionOption>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault, CreateEvent, accounts, instruments, holdings);

    private static HoldingPositionOptionCreatedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, AccountID accountID, InstrumentID instrumentID, string name, bool active, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, accountID, instrumentID, name, active, isDefault);
}
