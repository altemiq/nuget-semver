// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System.CommandLine.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// The console application.
    /// </summary>
    internal static partial class ConsoleApplication
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
        /// The default for the package ID Regex option.
        /// </summary>
        public const string? DefaultPackageIdRegex = default;

        /// <summary>
        /// The default for the package ID replace option.
        /// </summary>
        public const string DefaultPackageIdReplace = default;

        /// <summary>
        /// The default for the version suffix option.
        /// </summary>
        public const string? DefaultVersionSuffix = "";

        /// <summary>
        /// The default for the previous version option.
        /// </summary>
        public const NuGet.Versioning.SemanticVersion? DefaultPrevious = default;

        /// <summary>
        /// The default for the no version suffix option.
        /// </summary>
        public const bool DefaultNoVersionSuffix = default;

        /// <summary>
        /// The default for the no cache option.
        /// </summary>
        public const bool DefaultNoCache = default;

        /// <summary>
        /// The default for the direct download option.
        /// </summary>
        public const bool DefaultDirectDownload = default;

        /// <summary>
        /// The default for the configuration option.
        /// </summary>
        public const string? DefaultConfiguration = default;

        /// <summary>
        /// The default for the platform option.
        /// </summary>
        public const string? DefaultPlatform = default;

        private static bool isRegistered;

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
            System.CommandLine.IConsole console,
            System.IO.FileInfo first,
            System.IO.FileInfo second,
            NuGet.Versioning.SemanticVersion previous,
            string build,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo);

        /// <summary>
        /// The process project of solution delegate.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="projectOrSolution">The project or solution.</param>
        /// <param name="source">The NuGet source.</param>
        /// <param name="packageId">The package ID.</param>
        /// <param name="exclude">The values to exclude.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="platform">The platform.</param>
        /// <param name="packageIdRegex">The package ID regex.</param>
        /// <param name="packageIdReplace">The package ID replacement value.</param>
        /// <param name="versionSuffix">The version suffix.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="noVersionSuffix">Set to <see langword="true"/> to force there to be no version suffix.</param>
        /// <param name="noCache">Set to <see langword="true"/> to disable using the machine cache as the first package source.</param>
        /// <param name="directDownload">Set to <see langword="true"/> to download directly without populating any caches with metadata or binaries.</param>
        /// <param name="output">The output type.</param>
        /// <param name="buildNumberParameter">The parameter name for the build number.</param>
        /// <param name="versionSuffixParameter">The parameter name for the version suffix.</param>
        /// <param name="noLogo">Set to <see langword="true"/> to not display the startup banner or the copyright message.</param>
        /// <returns>The task.</returns>
        public delegate Task<int> ProcessProjectOrSolutionDelegate(
            System.CommandLine.IConsole console,
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? configuration = DefaultConfiguration,
            string? platform = DefaultPlatform,
            string? packageIdRegex = DefaultPackageIdRegex,
            string packageIdReplace = DefaultPackageIdReplace,
            string? versionSuffix = DefaultVersionSuffix,
            NuGet.Versioning.SemanticVersion? previous = DefaultPrevious,
            bool noVersionSuffix = DefaultNoVersionSuffix,
            bool noCache = DefaultNoCache,
            bool directDownload = DefaultDirectDownload,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo);

        /// <inheritdoc cref="FileFunctionDelegate" />
        public static void FileFunction(
            System.CommandLine.IConsole console,
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
            MSBuildApplication.WriteChanges(output, differences);
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

        /// <inheritdoc cref="ProcessProjectOrSolutionDelegate" />
        public static async Task<int> ProcessProjectOrSolution(
            System.CommandLine.IConsole console,
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? configuration = DefaultConfiguration,
            string? platform = DefaultPlatform,
            string? packageIdRegex = DefaultPackageIdRegex,
            string packageIdReplace = DefaultPackageIdReplace,
            string? versionSuffix = DefaultVersionSuffix,
            NuGet.Versioning.SemanticVersion? previous = DefaultPrevious,
            bool noVersionSuffix = DefaultNoVersionSuffix,
            bool noCache = DefaultNoCache,
            bool directDownload = DefaultDirectDownload,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo)
        {
            if (!noLogo)
            {
                WriteHeader(console);
            }

            var instance = RegisterMSBuild(projectOrSolution);
            if (output.HasFlag(OutputTypes.Diagnostic))
            {
                console.Out.WriteLine($"Using {instance.Name} {instance.Version}");
            }

            var version = await MSBuildApplication.ProcessProjectOrSolution(
                new ConsoleLogger(console, output.HasFlag(OutputTypes.Diagnostic)),
                projectOrSolution,
                configuration,
                platform,
                source,
                packageId,
                exclude,
                packageIdRegex,
                packageIdReplace,
                string.IsNullOrEmpty(versionSuffix) ? default : versionSuffix,
                previous,
                noVersionSuffix,
                noCache,
                directDownload,
                output,
                buildNumberParameter,
                versionSuffixParameter).ConfigureAwait(false);

            // write out the version and the suffix
            if (output.HasFlag(OutputTypes.TeamCity))
            {
                WriteTeamCityVersion(console, version, buildNumberParameter, versionSuffixParameter);
            }

            if (output.HasFlag(OutputTypes.Json))
            {
                WriteJsonVersion(console, version);
            }

            return 0;

            static Microsoft.Build.Locator.VisualStudioInstance RegisterMSBuild(System.IO.FileSystemInfo projectOrSolution)
            {
                var finder = new VisualStudioInstanceFinder(GetInstances());
                var instance = finder.GetVisualStudioInstance(projectOrSolution);
                if (!isRegistered)
                {
                    isRegistered = true;
                    Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);
                }

                return instance;

                static System.Collections.Generic.IEnumerable<Microsoft.Build.Locator.VisualStudioInstance> GetInstances()
                {
                    return Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances(new Microsoft.Build.Locator.VisualStudioInstanceQueryOptions
                    {
                        DiscoveryTypes = Microsoft.Build.Locator.DiscoveryType.DotNetSdk,
                    });
                }
            }
        }

        private static void WriteHeader(System.CommandLine.IConsole console)
        {
            console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Logo, VersionUtils.GetVersion()));
            console.Out.WriteLine(Properties.Resources.Copyright);
        }
    }
}