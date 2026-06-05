using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserDisplayPreferences : IType
{
    public required bool DarkMode { get; init; }

    public required bool RememberTraceDate { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public UserDisplayPreferences(bool darkMode, bool rememberTraceDate)
    {
        DarkMode = darkMode;
        RememberTraceDate = rememberTraceDate;
    }
}
