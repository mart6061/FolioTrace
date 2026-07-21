namespace Services;

/// <summary>
/// The serialized form of one holding's totals inside an AggregateSnapshot's PayloadJson for
/// AggregateKind "HoldingPositions" - deliberately just the computed numeric totals and identity, not the
/// display metadata (names, active flag) baked into HoldingPosition, since that's refreshed from current
/// Holdings/Accounts/Instruments at reconstruction time regardless of snapshot age.
/// </summary>
public sealed record HoldingPositionSnapshotItem(Guid HoldingID, decimal Quantity, decimal BookCost, Guid? LastEventID, DateTime? LastAuditDateTime);
