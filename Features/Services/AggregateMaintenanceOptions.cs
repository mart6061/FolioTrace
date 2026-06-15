namespace Services;

public sealed class AggregateMaintenanceOptions
{
    public const string SectionName = "AggregateMaintenance";

    public bool Enabled { get; set; } = true;

    public TimeSpan PeriodicDelay { get; set; } = TimeSpan.FromMinutes(10);

    public int EventTriggerCount { get; set; } = 100;

    public TimeSpan EventTriggerDelay { get; set; } = TimeSpan.FromSeconds(30);

    public AggregateMaintenanceDateWindowOptions DateWindows { get; set; } = new();
}
