using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Services;

public sealed class InputPolicyService(InputControlSettingsService inputControlSettingsService, CurrencyService currencyService)
{
    public async Task<IReadOnlyList<InputControlPolicy>> Resolve(InputPolicyResolveRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var settings = request.AuditDateTime is null
            ? await inputControlSettingsService.Get(request.EventDateTime)
            : await inputControlSettingsService.Get(request.EventDateTime, request.AuditDateTime);
        var currencies = request.AuditDateTime is null
            ? await currencyService.Get(request.EventDateTime)
            : await currencyService.Get(request.EventDateTime, request.AuditDateTime);

        return request.ControlKinds
            .Distinct()
            .Select(controlKind => InputPolicyResolver.Resolve(
                controlKind,
                settings.Items,
                currencies.Items,
                request.AccountID,
                request.UserID,
                request.Currency,
                request.AllowNegative))
            .ToList();
    }
}
