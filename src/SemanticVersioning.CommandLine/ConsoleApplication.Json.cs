// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.Json.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using System.CommandLine.IO;

    /// <content>
    /// Application class for writing the GitHub version.
    /// </content>
    internal static partial class ConsoleApplication
    {
        /// <summary>
        /// Writes the JSON version.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="version">The version to write.</param>
        public static void WriteJsonVersion(IConsoleWithOutput console, NuGet.Versioning.SemanticVersion version)
        {
            // export these as environment variables
            var versions = new Versions
            {
                Version = version,
                VersionPrefix = version.ToString("x.y.z", NuGet.Versioning.VersionFormatter.Instance),
                VersionSuffix = version.ToString("R", NuGet.Versioning.VersionFormatter.Instance),
            };

            var options = new System.Text.Json.JsonSerializerOptions { Converters = { new SemanticVersionConverter() } };
            console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(versions, typeof(Versions), options), OutputTypes.Json);
        }

        private class Versions
        {
            public NuGet.Versioning.SemanticVersion? Version { get; set; }

            public string? VersionPrefix { get; set; }

            public string? VersionSuffix { get; set; }
        }
    }
}