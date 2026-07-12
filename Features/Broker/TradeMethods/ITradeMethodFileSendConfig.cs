namespace FolioTrace.Aggregates;

[System.Text.Json.Serialization.JsonConverter(typeof(TradeMethodFileSendConfigJsonConverter))]
public interface ITradeMethodFileSendConfig;
