// -----------------------------------------------------------------------
// <copyright file="VisualStudioInstanceFinder.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The <see cref="Microsoft.Build.Locator.VisualStudioInstance"/> finder.
/// </summary>
internal class VisualStudioInstanceFinder
{
    private readonly IDictionary<NuGet.Versioning.SemanticVersion, Microsoft.Build.Locator.VisualStudioInstance> instances;

    /// <summary>
    /// Initialises a new instance of the <see cref="VisualStudioInstanceFinder"/> class.
    /// </summary>
    public VisualStudioInstanceFinder()
        : this(Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances())
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="VisualStudioInstanceFinder"/> class.
    /// </summary>
    /// <param name="instances">The available instances.</param>
    public VisualStudioInstanceFinder(IEnumerable<Microsoft.Build.Locator.VisualStudioInstance> instances) => this.instances = instances.ToDictionary(instance => SemanticVersion.Create(instance.Version), NuGet.Versioning.VersionComparer.VersionRelease);

    /// <summary>
    /// Gets the visual studio instance.
    /// </summary>
    /// <param name="path">The project or solution path.</param>
    /// <returns>The visual studio instance.</returns>
    public Microsoft.Build.Locator.VisualStudioInstance GetVisualStudioInstance(FileSystemInfo? path)
    {
        var instance = FindGlobalJson(path) is string globalJson
            ? this.GetVisualStudioInstance(globalJson)
            : this.instances.Select(instance => instance.Value).FirstOrDefault();

        return instance ?? throw new InvalidOperationException("No instances of MSBuild could be detected.");

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
    /// Gets the visual studio instance.
    /// </summary>
    /// <param name="globalJson">The global.json path.</param>
    /// <returns>The visual studio instance.</returns>
    public Microsoft.Build.Locator.VisualStudioInstance GetVisualStudioInstance(string globalJson)
    {
        var global = System.Text.Json.JsonSerializer.Deserialize<Global>(
            File.ReadAllText(globalJson),
            new System.Text.Json.JsonSerializerOptions
            {
                Converters = { SemanticVersionConverter.Instance, RollForwardPolicyConverter.Instance },
                PropertyNameCaseInsensitive = true,
            });

        if (global?.Sdk is not null)
        {
            var sdk = global.Sdk;

            return this.GetVisualStudioInstance(
                globalJson,
                sdk.Version,
                sdk.AllowPrerelease,
                sdk.RollForward ?? GetDefaultRollForwardPolicy(sdk.Version));
        }

        throw new InvalidOperationException("Failed to read SDK object from global.json");

        static RollForwardPolicy GetDefaultRollForwardPolicy(NuGet.Versioning.SemanticVersion? version)
        {
            return version is null
                ? RollForwardPolicy.LatestMajor
                : RollForwardPolicy.LatestPatch;
        }
    }

    /// <summary>
    /// Gets the visual studio instance.
    /// </summary>
    /// <param name="requested">The requested version.</param>
    /// <param name="allowPrerelease">Set to <see langword="true"/> to allow prerelease versions.</param>
    /// <param name="policy">The roll-forward policy.</param>
    /// <returns>The visual studio instance.</returns>
    public Microsoft.Build.Locator.VisualStudioInstance GetVisualStudioInstance(NuGet.Versioning.SemanticVersion? requested, bool allowPrerelease, RollForwardPolicy policy) => this.GetVisualStudioInstance("global.json", requested, allowPrerelease, policy);

    private Microsoft.Build.Locator.VisualStudioInstance GetVisualStudioInstance(string globalJson, NuGet.Versioning.SemanticVersion? requested, bool allowPrerelease, RollForwardPolicy policy)
    {
        if (ExactMatchPreferred(policy)
            && requested is not null
            && this.instances.TryGetValue(requested, out var instance)
            && instance is not null)
        {
            return instance;
        }

        if (policy == RollForwardPolicy.Disable)
        {
            throw new InvalidOperationException(FormattableString.Invariant($"The specified dotnet SDK from global.json version: [{requested}] from [{globalJson}] was not found."));
        }

        // find the patch version
        var validInstances = this.instances.Where(kvp => kvp.Key.MatchesPolicy(requested, allowPrerelease, policy))
            .OrderByDescending(instance => instance.Key, new RollForwardComparer(policy))
            .ToArray();

        if (validInstances.Length == 0)
        {
            throw new InvalidOperationException(FormattableString.Invariant($"A compatible installed dotnet SDK for global.json version: [{requested}] from [{globalJson}] was not found{Environment.NewLine}Please install the [{requested}] SDK or update [{globalJson}] with an installed dotnet SDK:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", this.instances.Select(instance => FormattableString.Invariant($"{instance.Value.Version} [{instance.Value.MSBuildPath}]")))}"));
        }

        return validInstances[0].Value;

        static bool ExactMatchPreferred(RollForwardPolicy rollForward)
        {
            return rollForward == RollForwardPolicy.Disable ||
                   rollForward == RollForwardPolicy.Patch;
        }
    }

    private sealed class RollForwardComparer : IComparer<NuGet.Versioning.SemanticVersion>
    {
        private readonly RollForwardPolicy policy;

        public RollForwardComparer(RollForwardPolicy policy) => this.policy = policy;

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

            return x.IsBetterMatch(y, this.policy) ? 1 : -1;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0097:A class that implements IComparable<T> or IComparable should override comparison operators", Justification = "This is not required")]
    private sealed class SemanticVersion : NuGet.Versioning.SemanticVersion
    {
        private SemanticVersion(Version version)
            : base(version)
        {
        }

        public static NuGet.Versioning.SemanticVersion Create(Version version) => new SemanticVersion(version);
    }

    private sealed class RollForwardPolicyConverter : System.Text.Json.Serialization.JsonConverter<RollForwardPolicy>
    {
        public static readonly System.Text.Json.Serialization.JsonConverter Instance = new RollForwardPolicyConverter();

        public override RollForwardPolicy Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
            reader.GetString() is string value
#if NETFRAMEWORK
                ? (RollForwardPolicy)Enum.Parse(typeof(RollForwardPolicy), value, ignoreCase: true)
#else
                ? Enum.Parse<RollForwardPolicy>(value, ignoreCase: true)
#endif
                : RollForwardPolicy.Unsupported;

        public override void Write(System.Text.Json.Utf8JsonWriter writer, RollForwardPolicy value, System.Text.Json.JsonSerializerOptions options) =>
            writer.WriteStringValue(System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(value.ToString()));
    }

    private sealed record Global(Sdk? Sdk);

    private sealed record Sdk(NuGet.Versioning.SemanticVersion? Version, bool AllowPrerelease = true, RollForwardPolicy? RollForward = default);
}