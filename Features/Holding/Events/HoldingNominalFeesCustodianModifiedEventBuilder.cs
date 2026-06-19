using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class HoldingNominalFeesCustodianModifiedEventBuilder
{
    public static Result<HoldingNominalFeesCustodianModifiedEvent> Create(HoldingNominalFeesCustodianModifiedRequest request, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.Create<HoldingNominalFeesCustodianModifiedEvent, HoldingNominalFeesCustodian>(request, CreateEvent, holdings);

    public static Result<HoldingNominalFeesCustodianModifiedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault, Holdings? holdings = null) =>
        HoldingModifiedEventBuilderCore.CreateSeed<HoldingNominalFeesCustodianModifiedEvent, HoldingNominalFeesCustodian>(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault, CreateEvent, holdings);

    private static HoldingNominalFeesCustodianModifiedEvent CreateEvent(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, HoldingID holdingID, string name, bool isDefault) =>
        new(eventId, userId, eventDateTime, auditDateTime, reason, holdingID, name, isDefault);
}
