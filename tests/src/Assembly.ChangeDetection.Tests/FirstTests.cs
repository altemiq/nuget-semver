// -----------------------------------------------------------------------
// <copyright file="FirstTests.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection;

public class FirstTests
{
    private static readonly string BaseAssembly = GetPath(Path.Combine("projects", "Original"), "Original.dll");

    [Fact]
    public void BreakingChanges() => Assert.Equivalent(
        new { ResultsType = SemVer.ResultsType.Major, VersionNumber = "2.0.0-alpha" },
        SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, GetPath(Path.Combine("projects", "BreakingChange"), "Original.dll"), ["1.0.1-alpha"]));

    [Fact]
    public void NonBreakingChanges() => Assert.Equivalent(
        new { ResultsType = SemVer.ResultsType.Minor, VersionNumber = "1.1.0-alpha" }, SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, GetPath(Path.Combine("projects", "NonBreakingAdditiveChange"), "Original.dll"), ["1.0.1"]));

    [Fact]
    public void NonChanges() => Assert.Equivalent(
        new { ResultsType = SemVer.ResultsType.Patch, VersionNumber = "1.0.2-beta" },
        SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, GetPath(Path.Combine("projects", "Original"), "Original.dll"), ["1.0.1-beta"]));

    [Fact]
    public void NameChange()
    {
        var testAssembly = GetPath(Path.Combine("projects", "New"), "New.dll");
        Assert.Equivalent(
            new { ResultsType = SemVer.ResultsType.Major, VersionNumber = "2.0.0-alpha" },
            SemVer.SemanticVersionAnalyzer.Analyze(Path.Combine(Path.GetDirectoryName(BaseAssembly) ?? string.Empty, Path.GetFileName(testAssembly)), testAssembly, ["1.0.1"]));
    }

    private static string GetPath(string project, string name)
    {
        var currentPath = Path.GetDirectoryName(typeof(FirstTests).Assembly.Location);

        currentPath = Path.GetDirectoryName(currentPath);

        var configuration = Path.GetFileName(currentPath)!;
        currentPath = Path.GetDirectoryName(currentPath);

        var type = Path.GetFileName(currentPath)!;
        var testProjectDirectory = Path.GetDirectoryName(currentPath)!;

        var projectDirectory = Path.GetFullPath(Path.Combine(testProjectDirectory, "..", "..", project, type, configuration));

        var framework = Directory.EnumerateDirectories(projectDirectory).First();

        return Path.GetFullPath(Path.Combine(framework, name)).Replace('\\', Path.DirectorySeparatorChar);
    }
}