namespace FolioTrace.Aggregates;

public sealed record FTPTradeMethodFileSendConfig(string Host, int Port, string RemotePath, string UserName, SecretReference? CredentialReference) : ITradeMethodFileSendConfig;
