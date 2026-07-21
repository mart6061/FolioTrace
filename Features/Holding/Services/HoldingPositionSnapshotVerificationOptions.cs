namespace Services;

/// <summary>
/// Controls Aggregate-Snapshot-Scaling-Plan.md Phase 4's rollout-safety check: for a sample of
/// snapshot-seeded HoldingPositions reads, also compute a full from-scratch replay and compare, since a
/// subtly wrong snapshot-plus-delta result is worse than a slow full replay for financial position data.
/// Intended to stay enabled through the rollout of Phase 3, then be turned off (or the SampleRate turned
/// down) once confidence is established.
/// </summary>
public sealed class HoldingPositionSnapshotVerificationOptions
{
    public const string SectionName = "HoldingPositionSnapshotVerification";

    public bool Enabled { get; set; } = true;

    /// <summary>Fraction (0.0-1.0) of snapshot-seeded reads that also get verified against a full replay.</summary>
    public double SampleRate { get; set; } = 0.1;
}
