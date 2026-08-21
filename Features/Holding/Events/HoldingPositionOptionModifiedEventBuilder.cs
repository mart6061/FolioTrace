using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class HoldingPositionOptionModifiedEventBuilder
{
    public static Result<HoldingPositionOptionModifiedEvent> Create(HoldingPositionOptionModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.Create<HoldingPositionOptionModifiedEvent, HoldingPositionOption>(request, CreateEvent, holdings);

    public static Result<HoldingPositionOptionModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateSeed<HoldingPositionOptionModifiedEvent, HoldingPositionOption>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, CreateEvent, holdings);

    private static HoldingPositionOptionModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault);
}
