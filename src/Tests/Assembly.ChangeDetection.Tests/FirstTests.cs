// -----------------------------------------------------------------------
// <copyright file="FirstTests.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection;

public class FirstTests
{
    private static readonly string BaseAssembly = GetPath(Path.Combine("Projects", "Original"), "Original.dll");

    [Fact]
    public void BreakingChanges() => SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, GetPath(Path.Combine("Projects", "BreakingChange"), "Original.dll"), new[] { "1.0.1-alpha" })
        .Should().BeEquivalentTo(new
        {
            ResultsType = SemVer.ResultsType.Major,
            VersionNumber = "2.0.0-alpha"
        });

    [Fact]
    public void NonBreakingChanges() => SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, GetPath(Path.Combine("Projects", "NonBreakingAdditiveChange"), "Original.dll"), new[] { "1.0.1" })
        .Should().BeEquivalentTo(new
        {
            ResultsType = SemVer.ResultsType.Minor,
            VersionNumber = "1.1.0-alpha"
        });

    [Fact]
    public void NonChanges() => SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, GetPath(System.IO.Path.Combine("Projects", "Original"), "Original.dll"), new[] { "1.0.1-beta" })
        .Should().BeEquivalentTo(new
        {
            ResultsType = SemVer.ResultsType.Patch,
            VersionNumber = "1.0.2-beta"
        });

    [Fact]
    public void NameChange()
    {
        var testAssembly = GetPath(System.IO.Path.Combine("Projects", "New"), "New.dll");
        _ = SemVer.SemanticVersionAnalyzer.Analyze(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(BaseAssembly) ?? string.Empty, System.IO.Path.GetFileName(testAssembly)), testAssembly, new[] { "1.0.1" })
            .Should().BeEquivalentTo(new
            {
                ResultsType = SemVer.ResultsType.Major,
                VersionNumber = "2.0.0-alpha"
            });
    }

    private static string GetPath(string project, string name)
    {
        var currentPath = Path.GetDirectoryName(typeof(FirstTests).Assembly.Location);

        currentPath = Path.GetDirectoryName(currentPath);

        var configuration = Path.GetFileName(currentPath)!;
        currentPath = Path.GetDirectoryName(currentPath);

        var type = Path.GetFileName(currentPath)!;
        var testProjectDirectory = Path.GetDirectoryName(currentPath)!;

        var projectDirectory = Path.GetFullPath(Path.Combine(testProjectDirectory, "..", project, type, configuration));

        var framework = Directory.EnumerateDirectories(projectDirectory).First();

        return Path.GetFullPath(Path.Combine(framework, name)).Replace('\\', Path.DirectorySeparatorChar);
    }
}