namespace FolioTrace.Aggregates;

public sealed record DateRuleOption(Guid OptionID, DateControlOptionKind Kind, string Label, string? Expression, int DisplayOrder, bool IsDefault);
