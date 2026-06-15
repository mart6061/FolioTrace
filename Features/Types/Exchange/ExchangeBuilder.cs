using System.Text.Json;
using System.Text.Json.Serialization;

namespace FolioTrace.Types;

public static class ExchangeBuilder
{
    public static Exchange Create(string value) => new(value);
}
