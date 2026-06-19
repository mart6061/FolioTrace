using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingNominalInSpecieInModifiedEventBuilder
{
    public static Result<HoldingNominalInSpecieInModifiedEvent> Create(HoldingNominalInSpecieInModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.Create<HoldingNominalInSpecieInModifiedEvent, HoldingNominalInSpecieIn>(request, CreateEvent, holdings);

    public static Result<HoldingNominalInSpecieInModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateSeed<HoldingNominalInSpecieInModifiedEvent, HoldingNominalInSpecieIn>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, CreateEvent, holdings);

    private static HoldingNominalInSpecieInModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault);
}
