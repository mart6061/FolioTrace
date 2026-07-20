using System.Collections.Concurrent;
using System.Text.Json;
using FoleoTrader.Models;
using Microsoft.Extensions.Options;
using QuickFix;
using QuickFix.Fields;

namespace FoleoTrader;

public sealed class FoleoTraderMessageMonitor
{
    private readonly ConcurrentQueue<FoleoTraderMessageEntry> entries = new();
    private readonly string monitorPath;

    public FoleoTraderMessageMonitor(IOptions<FoleoTraderAcceptorOptions> options)
    {
        monitorPath = options.Value.MonitorPath;
        var directory = Path.GetDirectoryName(monitorPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    public IReadOnlyList<FoleoTraderMessageEntry> Entries => entries.Reverse().Take(200).ToList();

    public void Record(string direction, Message message, SessionID sessionID)
    {
        var raw = message.ToString().Replace('\u0001', '|');
        var messageType = message.Header.IsSetField(Tags.MsgType) ? message.Header.GetString(Tags.MsgType) : string.Empty;
        var entry = new FoleoTraderMessageEntry(
            DateTime.UtcNow,
            direction,
            sessionID.ToString(),
            messageType,
            raw,
            ReadFields(message));

        entries.Enqueue(entry);
        while (entries.Count > 500 && entries.TryDequeue(out _))
        {
        }

        File.AppendAllText(monitorPath, JsonSerializer.Serialize(entry) + Environment.NewLine);
    }

    private static IReadOnlyList<FoleoTraderMessageField> ReadFields(Message message)
    {
        var fields = new List<FoleoTraderMessageField>();
        AddFields(fields, message.Header);
        AddFields(fields, message);
        AddFields(fields, message.Trailer);
        return fields;
    }

    private static void AddFields(List<FoleoTraderMessageField> fields, FieldMap map)
    {
        foreach (var field in map)
        {
            var tag = field.Key;
            fields.Add(new FoleoTraderMessageField(tag.ToString(), FieldName(tag), field.Value.ToString() ?? string.Empty));
        }
    }

    private static string FieldName(int tag) =>
        tag switch
        {
            Tags.BeginString => "BeginString",
            Tags.BodyLength => "BodyLength",
            Tags.MsgType => "MsgType",
            Tags.SenderCompID => "SenderCompID",
            Tags.TargetCompID => "TargetCompID",
            Tags.MsgSeqNum => "MsgSeqNum",
            Tags.SendingTime => "SendingTime",
            Tags.ClOrdID => "ClOrdID",
            Tags.OrderID => "OrderID",
            Tags.ExecID => "ExecID",
            Tags.ExecType => "ExecType",
            Tags.OrdStatus => "OrdStatus",
            Tags.Symbol => "Symbol",
            Tags.SecurityID => "SecurityID",
            Tags.SecurityIDSource => "SecurityIDSource",
            Tags.Side => "Side",
            Tags.OrderQty => "OrderQty",
            Tags.Price => "Price",
            Tags.LastQty => "LastQty",
            Tags.LastPx => "LastPx",
            Tags.CumQty => "CumQty",
            Tags.LeavesQty => "LeavesQty",
            Tags.GrossTradeAmt => "GrossTradeAmt",
            Tags.Currency => "Currency",
            Tags.OrdType => "OrdType",
            Tags.TransactTime => "TransactTime",
            Tags.CheckSum => "CheckSum",
            _ => string.Empty
        };
}
