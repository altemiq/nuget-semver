// -----------------------------------------------------------------------
// <copyright file="MSBuildTests.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.MSBuild;

using System;
using System.Collections.Generic;
using System.Text;

public class MSBuildTests
{
    [Test]
    public async Task RunMSBuild()
    {
        var projectFile = PathHelper.GetProjectPath("Original.MSBuild");

        var (result, properties) = MSBuildTestHelper.BuildProject(projectFile);

        await Assert.That(result.OverallResult).IsEqualTo(Microsoft.Build.Execution.BuildResultCode.Success);
        await Assert.That(properties)
            .ContainsKeyWithValue("Version", "1.0.1-main").And
            .ContainsKeyWithValue("VersionPrefix", "1.0.1").And
            .ContainsKeyWithValue("VersionSuffix", "main").And
            .ContainsKeyWithValue("PackageVersion", "1.0.1-main").And
            .ContainsKeyWithValue("NuGetVersion", "1.0.1-main");
    }
}