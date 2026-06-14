using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class ReportModifiedEventBuilder
{
    public static Result<ReportModifiedEvent> Create(ReportModifiedRequest request, ReportConfigs? reportConfigs = null, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(
            request.UserID,
            request.EventDateTime,
            request.Reason,
            request.ReportID,
            request.Name,
            request.Active,
            request.EffectiveDateTime,
            request.Nodes,
            reportConfigs,
            valuationSettings);
    }

    public static Result<ReportModifiedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, ReportID reportID, string name, bool active, EventDateTime effectiveDateTime, List<ReportNodeBase> nodes, ReportConfigs? reportConfigs = null, ValuationSettings? valuationSettings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var normalizedNodes = ReportConfigBuilder.NormaliseNodes(nodes);
        var validationErrors = ReportEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, reportID);
        ReportEventValidation.ValidateExistingReport(validationErrors, reportID, reportConfigs);
        ReportEventValidation.ValidateDefinition(validationErrors, name, effectiveDateTime, normalizedNodes, valuationSettings);

        return validationErrors.Count == 0
            ? Result<ReportModifiedEvent>.Success(new ReportModifiedEvent(eventId, userId, eventDateTime, auditDateTime, reason, reportID, name, active, effectiveDateTime, normalizedNodes))
            : Result<ReportModifiedEvent>.Invalid(validationErrors);
    }
}
