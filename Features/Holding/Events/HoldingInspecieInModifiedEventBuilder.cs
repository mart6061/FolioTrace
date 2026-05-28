using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingInspecieInModifiedEventBuilder
{
    public static Result<HoldingInspecieInModifiedEvent> Create(HoldingInspecieInModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.Create<HoldingInspecieInModifiedEvent, HoldingInspecieIn>(request, CreateEvent, holdings);

    public static Result<HoldingInspecieInModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateSeed<HoldingInspecieInModifiedEvent, HoldingInspecieIn>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, CreateEvent, holdings);

    private static HoldingInspecieInModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault);
}
