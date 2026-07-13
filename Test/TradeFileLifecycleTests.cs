using FolioTrace.Aggregates;
using FolioTrace.Types;

namespace Test;

public sealed class TradeFileLifecycleTests
{
    private static readonly UserID UserID = new(Guid.Parse("6d21c6ec-2cf0-430d-987b-6c32c1903538"));
    private static readonly TradeFileID TradeFileID = new(Guid.Parse("019f5256-ab3e-7877-b924-331623368b20"));
    private static readonly LegalEntityIdentifier BrokerLEI = new("5493001KJTIIGC8Y1R12");
    private static readonly EventDateTime RequestEventDate = new(new DateTime(2026, 7, 11, 21, 59, 59, DateTimeKind.Utc));
    private static readonly EventDateTime CallbackEventDate = new(new DateTime(2026, 7, 11, 18, 1, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime RequestAuditDate = new(new DateTime(2026, 7, 11, 18, 0, 0, DateTimeKind.Utc));
    private static readonly AuditDateTime SentAuditDate = new(new DateTime(2026, 7, 11, 18, 0, 30, DateTimeKind.Utc));
    private static readonly AuditDateTime AcknowledgedAuditDate = new(new DateTime(2026, 7, 11, 18, 1, 1, DateTimeKind.Utc));

    [Fact]
    public void ReplayNormalizesLegacyCallbackDateAndPreservesCausalStatus()
    {
        var requested = CreateRequestedEvent();
        var sent = new TradeFileSentEvent(new(Guid.CreateGuid7()), UserID, RequestEventDate, SentAuditDate, "Send", TradeFileID);
        var acknowledged = new TradeFileAcknowledgedEvent(new(Guid.CreateGuid7()), UserID, CallbackEventDate, AcknowledgedAuditDate, "Acknowledge", TradeFileID, Guid.CreateGuid7());

        var aggregate = new TradeFiles(RequestEventDate, AcknowledgedAuditDate, [acknowledged, sent, requested]);

        Assert.Equal(TradeFileStatus.Acknowledged, Assert.Single(aggregate.Items).Status);
    }

    [Fact]
    public void ReplayBeforeRequestEffectiveDateDoesNotExposeLegacyCallback()
    {
        var requested = CreateRequestedEvent();
        var acknowledged = new TradeFileAcknowledgedEvent(new(Guid.CreateGuid7()), UserID, CallbackEventDate, AcknowledgedAuditDate, "Acknowledge", TradeFileID, Guid.CreateGuid7());
        var valuationBeforeRequest = new EventDateTime(RequestEventDate.Value.AddTicks(-1));

        var aggregate = new TradeFiles(valuationBeforeRequest, AcknowledgedAuditDate, [acknowledged, requested]);

        Assert.Empty(aggregate.Items);
    }

    [Fact]
    public void ReplayStillRejectsGenuinelyOrphanedLifecycleEvent()
    {
        var acknowledged = new TradeFileAcknowledgedEvent(new(Guid.CreateGuid7()), UserID, CallbackEventDate, AcknowledgedAuditDate, "Acknowledge", TradeFileID, Guid.CreateGuid7());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new TradeFiles(RequestEventDate, AcknowledgedAuditDate, [acknowledged]));

        Assert.Contains($"Trade file '{TradeFileID}' was not found.", exception.Message);
    }

    private static TradeFileRequestedEvent CreateRequestedEvent() =>
        new(
            new(Guid.CreateGuid7()),
            UserID,
            RequestEventDate,
            RequestAuditDate,
            "Request",
            TradeFileID,
            BrokerLEI,
            "Broker",
            new FileNameTemplate("{brokername}.xlsx"),
            [TradeFileColumn.TicketID],
            new FTPTradeMethodFileSendConfig("localhost", 21, "/incoming", "user", null),
            []);
}
