using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[Builder]
public static class HoldingNominalFeesAdministratorModifiedEventBuilder
{
    public static Result<HoldingNominalFeesAdministratorModifiedEvent> Create(HoldingNominalFeesAdministratorModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.Create<HoldingNominalFeesAdministratorModifiedEvent, HoldingNominalFeesAdministrator>(request, CreateEvent, holdings);

    public static Result<HoldingNominalFeesAdministratorModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateSeed<HoldingNominalFeesAdministratorModifiedEvent, HoldingNominalFeesAdministrator>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, CreateEvent, holdings);

    private static HoldingNominalFeesAdministratorModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault);
}
