using System.Text.Json.Serialization;

namespace FolioTrace.Aggregates;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(EmailTradeMethodFileSendConfig), nameof(EmailTradeMethodFileSendConfig))]
[JsonDerivedType(typeof(FTPTradeMethodFileSendConfig), nameof(FTPTradeMethodFileSendConfig))]
public interface ITradeMethodFileSendConfig;
