// -----------------------------------------------------------------------
// <copyright file="NuGetVersionTests.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// <see cref="NuGetVersion"/> tests.
/// </summary>
public class NuGetVersionTests
{
    [Test]
    [MethodDataSource(nameof(GetVersionsToCalculate))]
    public async Task CalculateVersion(SemanticVersionChange change, string[] versions, string? prerelease, SemanticVersionIncrement increment, NuGet.Versioning.SemanticVersion expected) => await Assert.That(NuGetVersion.CalculateVersion(change, versions, prerelease, increment)).IsEqualTo(expected);

    public static IEnumerable<Func<(SemanticVersionChange, string[], string?, SemanticVersionIncrement, NuGet.Versioning.SemanticVersion)>> GetVersionsToCalculate()
    {
        const string AlphaPrelease = "alpha";
        const string DevelopmentPrelease = "develop";

        yield return () => (SemanticVersionChange.Major, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse("3.0.0"));
        yield return () => (SemanticVersionChange.Major, new[] { "3.0.5-develop", "2.0.0" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse("3.0.6"));
        yield return () => (SemanticVersionChange.Major, new[] { "2.0.5", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"3.0.0-{DevelopmentPrelease}"));
        yield return () => (SemanticVersionChange.Major, new[] { "3.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"3.0.6-{DevelopmentPrelease}"));

        yield return () => (SemanticVersionChange.Major, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse("3.0.0"));
        yield return () => (SemanticVersionChange.Major, new[] { "3.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"3.0.5-{DevelopmentPrelease}.0"));
        yield return () => (SemanticVersionChange.Major, new[] { "2.0.5", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"3.0.0-{DevelopmentPrelease}"));
        yield return () => (SemanticVersionChange.Major, new[] { "3.0.1-develop.45", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"3.0.1-{DevelopmentPrelease}.46"));

        yield return () => (SemanticVersionChange.Minor, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse("2.1.0"));
        yield return () => (SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse("2.1.6"));
        yield return () => (SemanticVersionChange.Minor, new[] { "2.1.5", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"2.2.0-{DevelopmentPrelease}"));
        yield return () => (SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"2.1.6-{DevelopmentPrelease}"));

        yield return () => (SemanticVersionChange.Minor, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse("2.1.0"));
        yield return () => (SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse("2.1.5"));
        yield return () => (SemanticVersionChange.Minor, new[] { "2.1.5", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"2.2.0-{DevelopmentPrelease}"));
        yield return () => (SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"2.1.5-{DevelopmentPrelease}.0"));
        yield return () => (SemanticVersionChange.Minor, new[] { "2.1.5-develop.15", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"2.1.5-{DevelopmentPrelease}.16"));

        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse("2.0.1"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.0-develop", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse("1.1.1"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse("2.0.6"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"2.0.6-{DevelopmentPrelease}"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.5-develop", "1.1.0", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"1.1.1-{DevelopmentPrelease}"));

        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse("2.0.1"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.0-develop", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse("1.1.1"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse("2.0.5"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"2.0.5-{DevelopmentPrelease}.0"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.5-develop", "1.1.0", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"1.1.1-{DevelopmentPrelease}"));
        yield return () => (SemanticVersionChange.Patch, new[] { "2.0.5-develop", "1.1.1-develop.21", "1.1.0", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, NuGet.Versioning.SemanticVersion.Parse($"1.1.1-{DevelopmentPrelease}.22"));

        yield return () => (SemanticVersionChange.Major, new[] { "0.1.2-alpha", "0.1.0-alpha" }, default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"0.1.3-{AlphaPrelease}"));
        yield return () => (SemanticVersionChange.Major, Array.Empty<string>(), default, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"0.1.0-{AlphaPrelease}"));
        yield return () => (SemanticVersionChange.Major, new[] { "0.1.2-alpha", "0.1.0-alpha" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"0.1.3-{DevelopmentPrelease}"));
        yield return () => (SemanticVersionChange.Major, Array.Empty<string>(), DevelopmentPrelease, SemanticVersionIncrement.Patch, NuGet.Versioning.SemanticVersion.Parse($"0.1.0-{DevelopmentPrelease}"));
    }
}