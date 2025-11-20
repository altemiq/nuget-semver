// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.Json.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

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
    public static void WriteJsonVersion(IConsoleWithOutput console, NuGet.Versioning.SemanticVersion? version)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(memoryStream))
        {
            writer.WriteStartObject();
            if (version is not null)
            {
                writer.WriteString("Version", version.ToFullString());
                if (version.ToString("x.y.z", NuGet.Versioning.VersionFormatter.Instance) is { } versionPrefix)
                {
                    writer.WriteString("VersionPrefix", versionPrefix);
                }

                if (version.ToString("R", NuGet.Versioning.VersionFormatter.Instance) is { } versionSuffix)
                {
                    writer.WriteString("VersionSuffix", versionSuffix);
                }
            }

            writer.WriteEndObject();
        }

        console.Out.WriteLine(
            System.Text.Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length),
            OutputTypes.Json);
    }
}