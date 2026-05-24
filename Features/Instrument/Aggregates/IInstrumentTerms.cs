using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

[JsonConverter(typeof(InstrumentTermsJsonConverter))]
public interface IInstrumentTerms : IType
{
    string TermsType { get; }
}
