// -----------------------------------------------------------------------
// <copyright file="Application.TeamCity.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System.CommandLine.IO;

    /// <content>
    /// Application class for writing the TeamCity version.
    /// </content>
    internal static partial class Application
    {
        private static void WriteTeamCityVersion(System.CommandLine.IConsole console, NuGet.Versioning.SemanticVersion version, string buildNumberParameter, string versionSuffixParameter)
        {
            if (buildNumberParameter.Contains(".", System.StringComparison.Ordinal))
            {
                console.Out.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:x.y.z}']", buildNumberParameter, version));
            }
            else
            {
                console.Out.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[{0} '{1:x.y.z}']", buildNumberParameter, version));
            }

            console.Out.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:R}']", versionSuffixParameter, version));
        }
    }
}