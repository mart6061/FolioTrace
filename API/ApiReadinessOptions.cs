namespace API;

public sealed class ApiReadinessOptions
{
    public const string SectionName = "ApiReadiness";
    public int StartupRetrySeconds { get; set; } = 3;
}
