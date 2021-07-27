// -----------------------------------------------------------------------
// <copyright file="Application.TeamCity.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using Microsoft.Extensions.Logging;

    /// <content>
    /// Application class for writing the TeamCity version.
    /// </content>
    public static partial class Application
    {
        /// <summary>
        /// Writes the team city version.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="version">The version.</param>
        /// <param name="buildNumberParameter">The build number parameter.</param>
        /// <param name="versionSuffixParameter">The version suffix parameter.</param>
        public static void WriteTeamCityVersion(ILogger logger, NuGet.Versioning.SemanticVersion version, string buildNumberParameter, string versionSuffixParameter)
        {
            if (buildNumberParameter
#if NETSTANDARD2_0
                .Contains("."))
#else
                .Contains(".", System.StringComparison.Ordinal))
#endif
            {
                logger.LogInformation(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:x.y.z}']", buildNumberParameter, version));
            }
            else
            {
                logger.LogInformation(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[{0} '{1:x.y.z}']", buildNumberParameter, version));
            }

            logger.LogInformation(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:R}']", versionSuffixParameter, version));
        }
    }
}