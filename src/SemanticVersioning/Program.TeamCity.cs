// -----------------------------------------------------------------------
// <copyright file="Program.TeamCity.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    /// <content>
    /// Program class for writing the TeamCity version.
    /// </content>
    internal static partial class Program
    {
        private static void WriteTeamCityVersion(NuGet.Versioning.SemanticVersion version, string buildNumberParameter, string versionSuffixParameter)
        {
            if (buildNumberParameter.Contains(".", System.StringComparison.Ordinal))
            {
                System.Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:x.y.z}']", buildNumberParameter, version));
            }
            else
            {
                System.Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[{0} '{1:x.y.z}']", buildNumberParameter, version));
            }

            System.Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:R}']", versionSuffixParameter, version));
        }
    }
}
