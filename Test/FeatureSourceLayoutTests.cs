using System.Text.RegularExpressions;

namespace Test;

public sealed class FeatureSourceLayoutTests
{
    private static readonly Regex TopLevelTypeDeclarationPattern = new(
        "^(?:(?:public|internal|file|abstract|sealed|static|partial|readonly)\\s+)*(?:(?:record)\\s+(?:class\\s+|struct\\s+)?|(?:class|interface|enum)\\s+)(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<generic>\\s*<[^>]+>)?",
        RegexOptions.Compiled);

    [Fact]
    public void FeatureSourceFilesContainOnlyOneTopLevelType()
    {
        var violations = FeatureSourceFiles()
            .Select(file => new
            {
                File = file,
                Declarations = TopLevelTypeDeclarations(file).ToList()
            })
            .Where(item => item.Declarations.Count > 1)
            .Select(item => $"{RelativePath(item.File)}: {string.Join(", ", item.Declarations.Select(declaration => declaration.Name))}")
            .Order()
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void FeatureSourceFileNamesMatchTopLevelTypes()
    {
        var violations = FeatureSourceFiles()
            .SelectMany(file => TopLevelTypeDeclarations(file)
                .Select(declaration => new
                {
                    File = file,
                    Declaration = declaration,
                    BaseName = Path.GetFileNameWithoutExtension(file)
                }))
            .Where(item => ExpectedFileBaseName(item.Declaration, item.BaseName) != item.BaseName)
            .Select(item => $"{RelativePath(item.File)}: expected {ExpectedFileBaseName(item.Declaration, item.BaseName)}.cs for {item.Declaration.Name}")
            .Order()
            .ToList();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> FeatureSourceFiles()
    {
        var featuresPath = Path.Combine(RepositoryRoot().FullName, "Features");
        return Directory.EnumerateFiles(featuresPath, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<TopLevelTypeDeclaration> TopLevelTypeDeclarations(string file)
    {
        var lineNumber = 0;
        foreach (var line in File.ReadLines(file))
        {
            lineNumber++;
            var match = TopLevelTypeDeclarationPattern.Match(line);
            if (match.Success)
                yield return new TopLevelTypeDeclaration(lineNumber, match.Groups["name"].Value, match.Groups["generic"].Value);
        }
    }

    private static string ExpectedFileBaseName(TopLevelTypeDeclaration declaration, string actualBaseName)
    {
        if (declaration.Name == "TicketEventBuilder" &&
            actualBaseName.StartsWith("Ticket", StringComparison.Ordinal) &&
            actualBaseName.EndsWith("Builder", StringComparison.Ordinal))
        {
            return actualBaseName;
        }

        if (declaration.Name == "IAggregateCacheInvalidator" &&
            declaration.Generic.Contains("TEvent", StringComparison.Ordinal))
        {
            return "IAggregateCacheInvalidatorOfTEvent";
        }

        return declaration.Name;
    }

    private static DirectoryInfo RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Features")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Test")))
            {
                return directory;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }

    private static string RelativePath(string file) =>
        Path.GetRelativePath(RepositoryRoot().FullName, file);

    private sealed record TopLevelTypeDeclaration(int LineNumber, string Name, string Generic);
}
