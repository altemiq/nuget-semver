// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.TeamCity.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <content>
/// Application class for writing the TeamCity version.
/// </content>
internal static partial class ConsoleApplication
{
    /// <summary>
    /// Writes the team city version.
    /// </summary>
    /// <param name="console">The console.</param>
    /// <param name="version">The version.</param>
    /// <param name="buildNumberParameter">The build number parameter.</param>
    /// <param name="versionSuffixParameter">The version suffix parameter.</param>
    public static void WriteTeamCityVersion(IConsoleWithOutput console, NuGet.Versioning.SemanticVersion version, string buildNumberParameter, string versionSuffixParameter)
    {
        if (buildNumberParameter
#if NETSTANDARD2_0
            .Contains("."))
#else
            .Contains(".", StringComparison.Ordinal))
#endif
        {
            console.Out.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:x.y.z}']", buildNumberParameter, version), OutputTypes.TeamCity);
        }
        else
        {
            console.Out.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[{0} '{1:x.y.z}']", buildNumberParameter, version), OutputTypes.TeamCity);
        }

        console.Out.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:R}']", versionSuffixParameter, version), OutputTypes.TeamCity);
    }
}