// -----------------------------------------------------------------------
// <copyright file="Application.Json.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using System.CommandLine.IO;

    /// <content>
    /// Application class for writing the GitHub version.
    /// </content>
    internal static partial class Application
    {
        private static void WriteJsonVersion(System.CommandLine.IConsole console, NuGet.Versioning.SemanticVersion version)
        {
            // export these as environment variables
            var versions = new Versions
            {
                Version = version,
                VersionPrefix = version.ToString("x.y.z", NuGet.Versioning.VersionFormatter.Instance),
                VersionSuffix = version.ToString("R", NuGet.Versioning.VersionFormatter.Instance),
            };

            var options = new System.Text.Json.JsonSerializerOptions { Converters = { new SemanticVersionConverter() } };
            console.Out.WriteLine(System.Text.Json.JsonSerializer.Serialize(versions, typeof(Versions), options));
        }

        private class Versions
        {
            public NuGet.Versioning.SemanticVersion? Version { get; set; }

            public string? VersionPrefix { get; set; }

            public string? VersionSuffix { get; set; }
        }

        private class SemanticVersionConverter : System.Text.Json.Serialization.JsonConverter<NuGet.Versioning.SemanticVersion>
        {
            public override NuGet.Versioning.SemanticVersion Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => NuGet.Versioning.SemanticVersion.Parse(reader.GetString());

            public override void Write(System.Text.Json.Utf8JsonWriter writer, NuGet.Versioning.SemanticVersion value, System.Text.Json.JsonSerializerOptions options) => writer.WriteStringValue(value.ToFullString());
        }
    }
}
