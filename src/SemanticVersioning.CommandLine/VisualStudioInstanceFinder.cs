// -----------------------------------------------------------------------
// <copyright file="VisualStudioInstanceFinder.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The <see cref="Microsoft.Build.Locator.VisualStudioInstance"/> finder.
/// </summary>
/// <param name="instances">The available instances.</param>
internal class VisualStudioInstanceFinder(IEnumerable<Microsoft.Build.Locator.VisualStudioInstance> instances)
{
    private readonly Dictionary<NuGet.Versioning.SemanticVersion, Microsoft.Build.Locator.VisualStudioInstance> instances = instances.ToDictionary(instance => SemanticVersion.Create(instance.Version), NuGet.Versioning.VersionComparer.VersionRelease);

    /// <summary>
    /// Initialises a new instance of the <see cref="VisualStudioInstanceFinder"/> class.
    /// </summary>
    public VisualStudioInstanceFinder()
        : this(Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances())
    {
    }

    /// <summary>
    /// Gets the Visual Studio instance.
    /// </summary>
    /// <param name="path">The project or solution path.</param>
    /// <returns>The visual studio instance.</returns>
    public Microsoft.Build.Locator.VisualStudioInstance GetVisualStudioInstance(FileSystemInfo? path)
    {
        var instance = FindGlobalJson(path) is { } globalJson
            ? this.GetVisualStudioInstance(globalJson)
            : default;

        return instance
               ?? this.instances.Select(static instance => instance.Value).FirstOrDefault()
               ?? throw new InvalidOperationException("No instances of MSBuild could be detected.");

        static string? FindGlobalJson(FileSystemInfo? path)
        {
            var directory = path switch
            {
                DirectoryInfo directoryInfo => directoryInfo.FullName,
                FileInfo fileInfo => fileInfo.DirectoryName,
                _ => Directory.GetCurrentDirectory(),
            };

            while (directory is not null)
            {
                var filePath = Path.Combine(directory, "global.json");
                if (File.Exists(filePath))
                {
                    return filePath;
                }

                directory = Path.GetDirectoryName(directory);
            }

            return default;
        }
    }

    /// <summary>
    /// Gets the Visual Studio instance.
    /// </summary>
    /// <param name="globalJson">The global.json path.</param>
    /// <returns>The visual studio instance.</returns>
    public Microsoft.Build.Locator.VisualStudioInstance? GetVisualStudioInstance(string globalJson)
    {
        // read this as a single JSON, no deserialization
        var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(globalJson));

        if (!document.RootElement.TryGetProperty("sdk", out var sdkProperty))
        {
            // it's acceptable for a global.json to not have an SDK
            return default;
        }

        // get the version, allow prerelease, and roll forward
        var version = sdkProperty.TryGetProperty("version", out var versionProperty)
                      && versionProperty.GetString() is { } stringVersion
                      && NuGet.Versioning.SemanticVersion.TryParse(stringVersion, out var parsedVersion)
            ? parsedVersion
            : default;

        bool allowPrerelease = !sdkProperty.TryGetProperty("allowPrerelease", out var allowPrereleaseProperty)
                               || !bool.TryParse(allowPrereleaseProperty.GetString(), out var parsedAllowPrerelease)
                               || parsedAllowPrerelease;

        RollForwardPolicy rollForward = sdkProperty.TryGetProperty("rollForward", out var rollForwardProperty)
                                        && Enum.TryParse<RollForwardPolicy>(rollForwardProperty.GetString(), ignoreCase: true, out var parsedRollForward)
            ? parsedRollForward
            : GetDefaultRollForwardPolicy(version);

        return this.GetVisualStudioInstance(globalJson, version, allowPrerelease, rollForward);

        static RollForwardPolicy GetDefaultRollForwardPolicy(NuGet.Versioning.SemanticVersion? version)
        {
            return version is null
                ? RollForwardPolicy.LatestMajor
                : RollForwardPolicy.LatestPatch;
        }
    }

    /// <summary>
    /// Gets the Visual Studio instance.
    /// </summary>
    /// <param name="requested">The requested version.</param>
    /// <param name="allowPrerelease">Set to <see langword="true"/> to allow prerelease versions.</param>
    /// <param name="policy">The roll-forward policy.</param>
    /// <returns>The visual studio instance.</returns>
    public Microsoft.Build.Locator.VisualStudioInstance GetVisualStudioInstance(NuGet.Versioning.SemanticVersion? requested, bool allowPrerelease, RollForwardPolicy policy) => this.GetVisualStudioInstance("global.json", requested, allowPrerelease, policy);

    private Microsoft.Build.Locator.VisualStudioInstance GetVisualStudioInstance(
        string globalJson,
        NuGet.Versioning.SemanticVersion? requested,
        bool allowPrerelease,
        RollForwardPolicy policy)
    {
        if (ExactMatchPreferred(policy)
            && requested is not null
            && this.instances.TryGetValue(requested, out var exactInstance))
        {
            return exactInstance;
        }

        if (policy is RollForwardPolicy.Disable)
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"The specified dotnet SDK from global.json version: [{requested}] from [{globalJson}] was not found."));
        }

        // find the patch version
        using var enumerator = this.instances.Where(kvp => kvp.Key.MatchesPolicy(requested, allowPrerelease, policy))
            .OrderByDescending(static instance => instance.Key, new RollForwardComparer(policy))
            .GetEnumerator();

        if (!enumerator.MoveNext())
        {
            throw new InvalidOperationException(FormattableString.Invariant(
                $"A compatible installed dotnet SDK for global.json version: [{requested}] from [{globalJson}] was not found{Environment.NewLine}Please install the [{requested}] SDK or update [{globalJson}] with an installed dotnet SDK:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", this.instances.Select(instance => FormattableString.Invariant($"{instance.Value.Version} [{instance.Value.MSBuildPath}]")))}"));
        }

        return enumerator.Current.Value;

        static bool ExactMatchPreferred(RollForwardPolicy rollForward)
        {
            return rollForward is RollForwardPolicy.Disable or RollForwardPolicy.Patch;
        }
    }

    private sealed class RollForwardComparer(RollForwardPolicy policy) : IComparer<NuGet.Versioning.SemanticVersion>
    {
        public int Compare(NuGet.Versioning.SemanticVersion? x, NuGet.Versioning.SemanticVersion? y)
        {
            if (x is null)
            {
                return y is null ? 0 : -1;
            }

            if (y is null)
            {
                return 1;
            }

            return x.IsBetterMatch(y, policy) ? 1 : -1;
        }
    }

    private sealed class SemanticVersion : NuGet.Versioning.SemanticVersion
    {
        private SemanticVersion(Version version)
            : base(version)
        {
        }

        public static NuGet.Versioning.SemanticVersion Create(Version version) => new SemanticVersion(version);
    }
}