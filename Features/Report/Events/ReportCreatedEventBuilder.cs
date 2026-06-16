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
            request.ReportID ?? ReportIDBuilder.Create(),
            request.Name,
            request.Active,
            request.Nodes,
            reportConfigs,
            valuationSettings);
    }

    public static Result<ReportCreatedEvent> Create(UserID userId, EventDateTime eventDateTime, string reason, ReportID reportID, string name, bool active, EventDateTime effectiveDateTime, List<ReportNodeBase> nodes, ReportConfigs? reportConfigs = null, ValuationSettings? valuationSettings = null)
    {
        return Create(userId, reportID, name, active, nodes, reportConfigs, valuationSettings);
    }

    public static Result<ReportCreatedEvent> Create(UserID userId, ReportID reportID, string name, bool active, List<ReportNodeBase> nodes, ReportConfigs? reportConfigs = null, ValuationSettings? valuationSettings = null)
    {
        var auditDateTime = AuditDateTimeBuilder.Create();
        EventID eventId = Guid.CreateGuid7();
        var normalizedNodes = ReportConfigBuilder.NormaliseNodes(nodes);
        var validationErrors = ReportEventValidation.ValidateBase(eventId, userId, auditDateTime, reportID);
        ReportEventValidation.ValidateDefinition(validationErrors, name, normalizedNodes, valuationSettings);
        ReportEventValidation.ValidateCreatedReport(validationErrors, reportID, reportConfigs);

        return validationErrors.Count == 0
            ? Result<ReportCreatedEvent>.Success(new ReportCreatedEvent(eventId, userId, auditDateTime, reportID, name, active, normalizedNodes))
            : Result<ReportCreatedEvent>.Invalid(validationErrors);
    }

    public static Result<ReportCreatedEvent> CreateSeed(EventID eventId, UserID userId, EventDateTime eventDateTime, AuditDateTime auditDateTime, string reason, ReportID reportID, string name, bool active, EventDateTime effectiveDateTime, List<ReportNodeBase> nodes, ReportConfigs? reportConfigs = null, ValuationSettings? valuationSettings = null)
    {
        var normalizedNodes = ReportConfigBuilder.NormaliseNodes(nodes);
        var validationErrors = ReportEventValidation.ValidateBase(eventId, userId, auditDateTime, reportID);
        ReportEventValidation.ValidateDefinition(validationErrors, name, normalizedNodes, valuationSettings);
        ReportEventValidation.ValidateCreatedReport(validationErrors, reportID, reportConfigs);

        return validationErrors.Count == 0
            ? Result<ReportCreatedEvent>.Success(new ReportCreatedEvent(eventId, userId, auditDateTime, reportID, name, active, normalizedNodes))
            : Result<ReportCreatedEvent>.Invalid(validationErrors);
    }
}
