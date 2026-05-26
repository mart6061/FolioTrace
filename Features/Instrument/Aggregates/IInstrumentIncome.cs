using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(InstrumentIncomeJsonConverter))]
public interface IInstrumentIncome : IType
{
    string IncomeType { get; }
}
