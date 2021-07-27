// -----------------------------------------------------------------------
// <copyright file="Application.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The application class.
    /// </summary>
    public static partial class Application
    {
        /// <summary>
        /// The default for the no logo option.
        /// </summary>
        public const bool DefaultNoLogo = default;

        /// <summary>
        /// The default for the output option.
        /// </summary>
        public const OutputTypes DefaultOutput = OutputTypes.TeamCity | OutputTypes.Diagnostic;

        /// <summary>
        /// The default for the build number parameter option.
        /// </summary>
        public const string DefaultBuildNumberParameter = "buildNumber";

        /// <summary>
        /// The default for the version suffix parameter option.
        /// </summary>
        public const string DefaultVersionSuffixParameter = "system.build.suffix";

        /// <summary>
        /// The file function delegate.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="first">The first file.</param>
        /// <param name="second">The second file.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="build">The build.</param>
        /// <param name="output">The output.</param>
        /// <param name="buildNumberParameter">The build number parameter.</param>
        /// <param name="versionSuffixParameter">The version suffix parameter.</param>
        /// <param name="noLogo">Set to <see langword="true"/> to not display the startup banner or the copyright message.</param>
        public delegate void FileFunctionDelegate(
            ILogger console,
            System.IO.FileInfo first,
            System.IO.FileInfo second,
            NuGet.Versioning.SemanticVersion previous,
            string build,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo);

        /// <inheritdoc cref="FileFunctionDelegate" />
        public static void FileFunction(
            ILogger console,
            System.IO.FileInfo first,
            System.IO.FileInfo second,
            NuGet.Versioning.SemanticVersion previous,
            string build,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo)
        {
            if (!noLogo)
            {
                WriteHeader(console);
            }

            (var version, _, var differences) = LibraryComparison.Analyze(first.FullName, second.FullName, new[] { previous.ToString() }, build);
            WriteChanges(output, differences);
            if (version is not null)
            {
                if (output.HasFlag(OutputTypes.TeamCity))
                {
                    WriteTeamCityVersion(console, version, buildNumberParameter, versionSuffixParameter);
                }

                if (output.HasFlag(OutputTypes.Json))
                {
                    WriteJsonVersion(console, version);
                }
            }
        }

        /// <summary>
        /// Writes the header.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public static void WriteHeader(ILogger logger)
        {
            logger.LogInformation(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Logo, VersionUtils.GetVersion()));
            logger.LogInformation(Properties.Resources.Copyright);
        }
    }
}