// -----------------------------------------------------------------------
// <copyright file="NuGetVersionTests.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.Tests
{
    using FluentAssertions;
    using Xunit;

    /// <summary>
    /// <see cref="NuGetVersion"/> tests.
    /// </summary>
    public class NuGetVersionTests
    {
        private const string AlphaPrelease = "alpha";

        private const string DevelopmentPrelease = "develop";

        /// <summary>
        /// Calculates the version.
        /// </summary>
        /// <param name="change">The desired change.</param>
        /// <param name="versions">The versions.</param>
        /// <param name="prerelease">The release.</param>
        /// <param name="increment">The increment value.</param>
        /// <param name="expected">The expected version.</param>
        [Theory]
        [InlineData(SemanticVersionChange.Major, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, "3.0.0")]
        [InlineData(SemanticVersionChange.Major, new[] { "3.0.5-develop", "2.0.0" }, default, SemanticVersionIncrement.Patch, "3.0.6")]
        [InlineData(SemanticVersionChange.Major, new[] { "2.0.5", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, $"3.0.0-{DevelopmentPrelease}")]
        [InlineData(SemanticVersionChange.Major, new[] { "3.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, $"3.0.6-{DevelopmentPrelease}")]

        [InlineData(SemanticVersionChange.Major, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, "3.0.0")]
        [InlineData(SemanticVersionChange.Major, new[] { "3.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"3.0.5-{DevelopmentPrelease}.0")]
        [InlineData(SemanticVersionChange.Major, new[] { "2.0.5", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"3.0.0-{DevelopmentPrelease}")]
        [InlineData(SemanticVersionChange.Major, new[] { "3.0.1-develop.45", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"3.0.1-{DevelopmentPrelease}.46")]

        [InlineData(SemanticVersionChange.Minor, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, "2.1.0")]
        [InlineData(SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, default, SemanticVersionIncrement.Patch, "2.1.6")]
        [InlineData(SemanticVersionChange.Minor, new[] { "2.1.5", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, $"2.2.0-{DevelopmentPrelease}")]
        [InlineData(SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, $"2.1.6-{DevelopmentPrelease}")]

        [InlineData(SemanticVersionChange.Minor, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, "2.1.0")]
        [InlineData(SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, "2.1.5")]
        [InlineData(SemanticVersionChange.Minor, new[] { "2.1.5", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"2.2.0-{DevelopmentPrelease}")]
        [InlineData(SemanticVersionChange.Minor, new[] { "2.1.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"2.1.5-{DevelopmentPrelease}.0")]
        [InlineData(SemanticVersionChange.Minor, new[] { "2.1.5-develop.15", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"2.1.5-{DevelopmentPrelease}.16")]

        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, "2.0.1")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.0-develop", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.Patch, "1.1.1")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, default, SemanticVersionIncrement.Patch, "2.0.6")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, $"2.0.6-{DevelopmentPrelease}")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.5-develop", "1.1.0", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, $"1.1.1-{DevelopmentPrelease}")]

        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.0", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, "2.0.1")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.0-develop", "1.1.0", "1.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, "1.1.1")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, default, SemanticVersionIncrement.ReleaseLabel, "2.0.5")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.5-develop", "2.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"2.0.5-{DevelopmentPrelease}.0")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.5-develop", "1.1.0", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"1.1.1-{DevelopmentPrelease}")]
        [InlineData(SemanticVersionChange.Patch, new[] { "2.0.5-develop", "1.1.1-develop.21", "1.1.0", "1.0.0" }, DevelopmentPrelease, SemanticVersionIncrement.ReleaseLabel, $"1.1.1-{DevelopmentPrelease}.22")]

        [InlineData(SemanticVersionChange.Major, new[] { "0.1.2-alpha", "0.1.0-alpha" }, default, SemanticVersionIncrement.Patch, $"0.1.3-{AlphaPrelease}")]
        [InlineData(SemanticVersionChange.Major, new string[0], default, SemanticVersionIncrement.Patch, $"0.1.0-{AlphaPrelease}")]
        [InlineData(SemanticVersionChange.Major, new[] { "0.1.2-alpha", "0.1.0-alpha" }, DevelopmentPrelease, SemanticVersionIncrement.Patch, $"0.1.3-{DevelopmentPrelease}")]
        [InlineData(SemanticVersionChange.Major, new string[0], DevelopmentPrelease, SemanticVersionIncrement.Patch, $"0.1.0-{DevelopmentPrelease}")]
        public void CalculateVersion(SemanticVersionChange change, string[] versions, string? prerelease, SemanticVersionIncrement increment, string expected) =>
            NuGetVersion.CalculateVersion(change, versions, prerelease, increment).Should().Be(NuGet.Versioning.SemanticVersion.Parse(expected));
    }
}