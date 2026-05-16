using FolioTrace.Types;

namespace FolioTrace;

public static class Constants
{
    public static class Initialisation
    {
        public const string Reason = "Setup";

        public static readonly Guid CountriesStreamId = Guid.Parse("6b9326f8-5d39-4f56-9817-b86cf8ed1e5a");

        public static readonly EventDateTime EventDateTime = EventDateTimeBuilder.Create(DateTime.MinValue.AddTicks(1));

        public static readonly AuditDateTime AuditDateTime = AuditDateTimeBuilder.Create(DateTime.MinValue.AddTicks(1));
    }
}
