namespace Services;

public sealed class AggregateMaintenanceOptions
{
    public const string SectionName = "AggregateMaintenance";

    public bool Enabled { get; set; } = true;

    public TimeSpan PeriodicDelay { get; set; } = TimeSpan.FromMinutes(10);

    public int EventTriggerCount { get; set; } = 100;

    public TimeSpan EventTriggerDelay { get; set; } = TimeSpan.FromSeconds(30);

    public AggregateMaintenanceDateWindowOptions DateWindows { get; set; } = new();

    /// <summary>
    /// Snapshot cadence tiers (Aggregate-Snapshot-Scaling-Plan.md 3.2): a valuation date younger than this
    /// many days is "hot" - it churns too often to be worth persisting a snapshot for, so it stays as
    /// in-memory rebuild-on-miss only. Anything older ("warm"/"cold") gets a persisted snapshot during the
    /// warm loop. There's no separate warm/cold distinction in code - both persist identically, "cold" is
    /// just the operational expectation that invalidation there should be rare enough to alert on.
    /// </summary>
    public int SnapshotEligibleAfterDays { get; set; } = 14;
}
