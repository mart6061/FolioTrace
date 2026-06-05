using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FolioTrace.Types;

namespace FolioTrace.Aggregates;

public sealed record UserMenuPreferenceItem : IType
{
    public required string MenuItemID { get; init; }

    public required bool Visible { get; init; }

    [JsonConstructor]
    [SetsRequiredMembers]
    public UserMenuPreferenceItem(string menuItemID, bool visible)
    {
        MenuItemID = menuItemID;
        Visible = visible;
    }
}
