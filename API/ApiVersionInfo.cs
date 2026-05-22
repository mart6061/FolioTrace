using System.Diagnostics;
using System.Reflection;

namespace API;

public sealed class ApiVersionInfo
{
    public string ApiVersion { get; } = CreateDisplayVersion(typeof(ApiVersionInfo).Assembly);

    private static string CreateDisplayVersion(Assembly assembly)
    {
        var baseVersion = GetBaseVersion(assembly);
        var buildNumber = TryRunGit("rev-list", "--count", "HEAD");
        var revisionHash = TryRunGit("rev-parse", "--short=4", "HEAD");
        var build = int.TryParse(buildNumber, out var parsedBuild)
            ? parsedBuild
            : baseVersion.Build < 0 ? 0 : baseVersion.Build;
        var revision = TryParseHexRevision(revisionHash, out var parsedRevision)
            ? parsedRevision
            : baseVersion.Revision < 0 ? 0 : baseVersion.Revision;

        return $"{baseVersion.Major}.{baseVersion.Minor}.{build}.{revision}";
    }

    private static Version GetBaseVersion(Assembly assembly)
    {
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var versionText = informationalVersion?.Split('+')[0].Split('-')[0];

        return Version.TryParse(versionText, out var version)
            ? version
            : assembly.GetName().Version ?? new Version(0, 0, 0, 0);
    }

    private static bool TryParseHexRevision(string? value, out int revision)
    {
        revision = 0;
        return !string.IsNullOrWhiteSpace(value)
            && int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out revision);
    }

    private static string? TryRunGit(params string[] arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = string.Join(' ', arguments),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory()
            });

            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            if (!process.WaitForExit(1_000))
            {
                process.Kill(true);
                return null;
            }

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
