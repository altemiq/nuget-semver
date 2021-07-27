// -----------------------------------------------------------------------
// <copyright file="Application.Json.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using Microsoft.Extensions.Logging;

    /// <content>
    /// Application class for writing the GitHub version.
    /// </content>
    public static partial class Application
    {
        /// <summary>
        /// Writes the JSON version.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="version">The version to write.</param>
        public static void WriteJsonVersion(ILogger logger, NuGet.Versioning.SemanticVersion version)
        {
            // export these as environment variables
            var versions = new Versions
            {
                Version = version,
                VersionPrefix = version.ToString("x.y.z", NuGet.Versioning.VersionFormatter.Instance),
                VersionSuffix = version.ToString("R", NuGet.Versioning.VersionFormatter.Instance),
            };

            var options = new System.Text.Json.JsonSerializerOptions { Converters = { new SemanticVersionConverter() } };
            logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(versions, typeof(Versions), options));
        }

        private class Versions
        {
            public NuGet.Versioning.SemanticVersion? Version { get; set; }

            public string? VersionPrefix { get; set; }

            public string? VersionSuffix { get; set; }
        }
    }
}