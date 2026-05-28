using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingInspecieOutModifiedEventBuilder
{
    public static Result<HoldingInspecieOutModifiedEvent> Create(HoldingInspecieOutModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.Create<HoldingInspecieOutModifiedEvent, HoldingInspecieOut>(request, CreateEvent, holdings);

    public static Result<HoldingInspecieOutModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateSeed<HoldingInspecieOutModifiedEvent, HoldingInspecieOut>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, CreateEvent, holdings);

    private static HoldingInspecieOutModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault);
}
