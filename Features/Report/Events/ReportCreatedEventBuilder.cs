using FolioTrace.Common;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public static class ReportCreatedEventBuilder
{
    public static Result<ReportCreatedEvent> Create(ReportCreatedRequest request, ReportConfigs? reportConfigs = null, ValuationSettings? valuationSettings = null)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return Create(
            request.UserID,
            request.EventDateTime,
            request.Reason,
            request.ReportID ?? ReportIDBuilder.Create(),
            request.Name,
            request.Active,
            request.EffectiveDateTime,
            request.Nodes,
            reportConfigs,
            valuationSettings);
    }

    public static Result<ReportCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, ReportID reportID, string name, bool active, EventDateTime effectiveDateTime, List<ReportNodeBase> nodes, ReportConfigs? reportConfigs = null, ValuationSettings? valuationSettings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var normalizedNodes = ReportConfigBuilder.NormaliseNodes(nodes);
        var validationErrors = ReportEventValidation.ValidateBase(eventId, userId, eventDateTime, auditDateTime, reason, reportID);
        ReportEventValidation.ValidateDefinition(validationErrors, name, effectiveDateTime, normalizedNodes, valuationSettings);
        ReportEventValidation.ValidateCreatedReport(validationErrors, reportID, reportConfigs);

        return validationErrors.Count == 0
            ? Result<ReportCreatedEvent>.Success(new ReportCreatedEvent(eventId, userId, eventDateTime, auditDateTime, reason, reportID, name, active, effectiveDateTime, normalizedNodes))
            : Result<ReportCreatedEvent>.Invalid(validationErrors);
    }
}
