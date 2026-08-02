namespace Services;

public sealed class ProfitLossSnapshotVerificationOptions
{
    public const string SectionName = "ProfitLossSnapshotVerification";

    public bool Enabled { get; set; } = true;

    public double SampleRate { get; set; } = 0.1;
}
