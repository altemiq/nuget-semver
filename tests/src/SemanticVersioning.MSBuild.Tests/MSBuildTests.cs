// -----------------------------------------------------------------------
// <copyright file="MSBuildTests.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.MSBuild;

public class MSBuildTests
{
    private readonly MSBuildTestHelper helper = new();

    [Test]
    [NotInParallel]
    public async Task Build()
    {
        var projectFile = PathHelper.GetProjectPath("Original.MSBuild");

        var (result, properties) = this.helper.BuildProject(projectFile);

        await Assert.That(result.OverallResult).IsEqualTo(Microsoft.Build.Execution.BuildResultCode.Success);
        await Assert.That(properties)
            .ContainsKeyWithValue("Version", "1.0.1-main").And
            .ContainsKeyWithValue("VersionPrefix", "1.0.1").And
            .ContainsKeyWithValue("VersionSuffix", "main").And
            .ContainsKeyWithValue("PackageVersion", "1.0.1-main").And
            .ContainsKeyWithValue("NuGetVersion", "1.0.1-main");
    }

    [Test]
    [NotInParallel]
    public async Task Pack()
    {
        var projectFile = PathHelper.GetProjectPath("Original.MSBuild");

        var (result, properties) = this.helper.BuildProject(projectFile, target: nameof(Pack));

        await Assert.That(result.OverallResult).IsEqualTo(Microsoft.Build.Execution.BuildResultCode.Success);
        await Assert.That(properties)
            .ContainsKeyWithValue("Version", "1.0.1").And
            .ContainsKeyWithValue("VersionPrefix", "1.0.1").And
            .ContainsKeyWithValue("VersionSuffix", "main").And
            .ContainsKeyWithValue("PackageVersion", "1.0.1-main").And
            .ContainsKeyWithValue("NuGetVersion", "1.0.1-main");
    }
}