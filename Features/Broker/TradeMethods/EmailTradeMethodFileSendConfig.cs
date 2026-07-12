namespace FolioTrace.Aggregates;

public sealed record EmailTradeMethodFileSendConfig(EmailAddress To) : ITradeMethodFileSendConfig;
