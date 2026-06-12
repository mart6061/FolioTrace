using FolioTrace;
using FolioTrace.Aggregates;
using FolioTrace.Types;
using QuickFix;
using QuickFix.Fields;
using Repository;
using System.Globalization;

namespace API.FoleoTrader;

public sealed class FoleoTraderFIXOperationRecorder(IEventRepository eventRepository)
{
    private const char Soh = '\u0001';

    public Task RecordAsync(string direction, string channel, Message message, SessionID sessionID, CancellationToken cancellationToken)
    {
        var recordedAtUtc = DateTime.UtcNow;
        var msgType = ReadHeaderString(message, Tags.MsgType);
        var @event = new FoleoTraderFIXOperationRecordedEvent(
            Guid.CreateGuid7(),
            Constants.Initialisation.UserID,
            EventDateTimeBuilder.Create(recordedAtUtc),
            AuditDateTimeBuilder.Create(recordedAtUtc),
            $"Record {direction.ToLowerInvariant()} FoleoTrader FIX {channel.ToLowerInvariant()} message{(string.IsNullOrWhiteSpace(msgType) ? string.Empty : $" {msgType}")}",
            direction,
            channel,
            sessionID.ToString(),
            msgType,
            ReadHeaderInt(message, Tags.MsgSeqNum),
            ReadHeaderString(message, Tags.SenderCompID),
            ReadHeaderString(message, Tags.TargetCompID),
            ReadHeaderTimestamp(message, Tags.SendingTime),
            ReadBodyString(message, Tags.ClOrdID),
            ReadBodyString(message, Tags.ExecID),
            NormalizeRawMessage(message.ToString()));

        return eventRepository.AppendAsync(Constants.Initialisation.FoleoTraderFIXOperationsStreamId, @event, cancellationToken);
    }

    private static string NormalizeRawMessage(string value) =>
        string.IsNullOrEmpty(value) || value[^1] == Soh ? value : $"{value}{Soh}";

    private static string ReadHeaderString(Message message, int tag)
    {
        try
        {
            return message.Header.IsSetField(tag) ? message.Header.GetString(tag) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ReadBodyString(Message message, int tag)
    {
        try
        {
            return message.IsSetField(tag) ? message.GetString(tag) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static int? ReadHeaderInt(Message message, int tag) =>
        int.TryParse(ReadHeaderString(message, tag), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static DateTime? ReadHeaderTimestamp(Message message, int tag)
    {
        var value = ReadHeaderString(message, tag);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string[] formats =
        [
            "yyyyMMdd-HH:mm:ss",
            "yyyyMMdd-HH:mm:ss.f",
            "yyyyMMdd-HH:mm:ss.ff",
            "yyyyMMdd-HH:mm:ss.fff",
            "yyyyMMdd-HH:mm:ss.ffff",
            "yyyyMMdd-HH:mm:ss.fffff",
            "yyyyMMdd-HH:mm:ss.ffffff",
            "yyyyMMdd-HH:mm:ss.fffffff"
        ];
        return DateTime.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;
    }
}
