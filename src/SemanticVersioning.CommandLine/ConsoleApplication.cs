// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The console application.
    /// </summary>
    internal static class ConsoleApplication
    {
        private static bool isRegistered;

        /// <inheritdoc cref="MSBuildApplication.ProcessProjectOrSolutionDelegate" />
        public static Task<int> ProcessProjectOrSolution(
            ILogger logger,
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? configuration = MSBuildApplication.DefaultConfiguration,
            string? platform = MSBuildApplication.DefaultPlatform,
            string? packageIdRegex = MSBuildApplication.DefaultPackageIdRegex,
            string packageIdReplace = MSBuildApplication.DefaultPackageIdReplace,
            string? versionSuffix = MSBuildApplication.DefaultVersionSuffix,
            NuGet.Versioning.SemanticVersion? previous = MSBuildApplication.DefaultPrevious,
            bool noVersionSuffix = MSBuildApplication.DefaultNoVersionSuffix,
            bool noCache = MSBuildApplication.DefaultNoCache,
            bool directDownload = MSBuildApplication.DefaultDirectDownload,
            OutputTypes output = Application.DefaultOutput,
            string buildNumberParameter = Application.DefaultBuildNumberParameter,
            string versionSuffixParameter = Application.DefaultVersionSuffixParameter,
            bool noLogo = Application.DefaultNoLogo)
        {
            if (!noLogo)
            {
                Application.WriteHeader(logger);
            }

            var instance = RegisterMSBuild(projectOrSolution);
            if (output.HasFlag(OutputTypes.Diagnostic))
            {
                logger.LogTrace($"Using {instance.Name} {instance.Version}");
            }

            return MSBuildApplication.ProcessProjectOrSolution(
                logger,
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
                versionSuffixParameter);

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
    }
}