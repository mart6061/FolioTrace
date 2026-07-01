using System.Text.Json;
using System.Text.Json.Serialization;

using FolioTrace.Common;

namespace FolioTrace.Types;

[Builder]
public static class ExchangeBuilder
{
    public static Exchange Create(string value) => new(value);
}
