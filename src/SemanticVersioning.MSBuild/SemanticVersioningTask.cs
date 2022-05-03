// -----------------------------------------------------------------------
// <copyright file="SemanticVersioningTask.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The semantic versioning task.
/// </summary>
public class SemanticVersioningTask : Microsoft.Build.Utilities.Task
{
    private SemanticVersionIncrement semanticVersionIncrement;

    /// <summary>
    /// Gets or sets the project directory.
    /// </summary>
    [Required]
    public string ProjectDir { get; set; } = default!;

    /// <summary>
    /// Gets or sets the assembly name.
    /// </summary>
    [Required]
    public string AssemblyName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the package ID.
    /// </summary>
    [Required]
    public string PackageId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the target extension.
    /// </summary>
    [Required]
    public string TargetExt { get; set; } = default!;

    /// <summary>
    /// Gets or sets the build output target folder.
    /// </summary>
    [Required]
    public string BuildOutputTargetFolder { get; set; } = default!;

    /// <summary>
    /// Gets or sets the output path without TFM.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = default!;

    /// <summary>
    /// Gets or sets the semicolon-delimited list of package sources.
    /// </summary>
    public string[] RestoreSources { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the package ID regular expression.
    /// </summary>
    public string? PackageIdRegex { get; set; }

    /// <summary>
    /// Gets or sets the package ID replace value.
    /// </summary>
    public string? PackageIdReplace { get; set; }

    /// <summary>
    /// Gets or sets the previous version.
    /// </summary>
    public string? Previous { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable using the machine cache as the first package source.
    /// </summary>
    public bool NoCache { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to download directly without populating any caches with metadata or binaries.
    /// </summary>
    public bool DirectDownload { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to force there to be no version suffix.
    /// </summary>
    public bool NoVersionSuffix { get; set; }

    /// <summary>
    /// Gets or sets the pre-release value. If none is specified, the pre-release from the previous version is used.
    /// </summary>
    public string? VersionSuffix { get; set; }

    /// <summary>
    /// Gets or sets the project commits.
    /// </summary>
    public string[] Commits { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the head commits.
    /// </summary>
    public string[] HeadCommits { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the latest reference commit.
    /// </summary>
    public string? ReferenceCommit { get; set; }

    /// <summary>
    /// Gets or sets the referenced packages.
    /// </summary>
    public ITaskItem[] ReferencedPackages { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Gets or sets the increment location.
    /// </summary>
    public string Increment
    {
        get => this.semanticVersionIncrement.ToString();
        set => this.semanticVersionIncrement = (SemanticVersionIncrement)Enum.Parse(typeof(SemanticVersionIncrement), value);
    }

    /// <summary>
    /// Gets the calculated semantic version.
    /// </summary>
    [Output]
    public string? ComputedVersion { get; private set; }

    /// <summary>
    /// Gets the calculated version prefix.
    /// </summary>
    [Output]
    public string? ComputedVersionPrefix { get; private set; }

    /// <summary>
    /// Gets the calculated version suffix.
    /// </summary>
    [Output]
    public string? ComputedVersionSuffix { get; private set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyHelper.ResolveInDirectory;

        var regex = this.PackageIdRegex is null
            ? null
            : new System.Text.RegularExpressions.Regex(this.PackageIdRegex, System.Text.RegularExpressions.RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));

        var previousVersion = this.Previous is null
            ? default
            : NuGet.Versioning.SemanticVersion.Parse(this.Previous);

        var restoreSources = this.RestoreSources ?? Array.Empty<string>();
        var projectCommmits = this.Commits ?? Array.Empty<string>();
        var headCommits = this.HeadCommits ?? Array.Empty<string>();
        if (projectCommmits.Length > 0)
        {
            var projectCommit = projectCommmits[0];
            headCommits = headCommits.TakeWhile(commit => !string.Equals(commit, projectCommit, StringComparison.Ordinal)).ToArray();
        }

        var versionSuffix = this.VersionSuffix?
            .Replace('/', '-');

        var referenceVersions = this.ReferencedPackages is null
            ? new List<PackageCommitIdentity>()
            : this.ReferencedPackages.Select(itemTask => new PackageCommitIdentity(itemTask.ItemSpec, NuGet.Versioning.NuGetVersion.Parse(itemTask.GetMetadata("Version")), itemTask.GetMetadata("Commit"))).ToList();

        var (packageId, differences, published) = MSBuildApplication.ProcessProject(
            this.ProjectDir,
            this.AssemblyName,
            this.PackageId,
            this.TargetExt,
            this.BuildOutputTargetFolder,
            this.OutputPath,
            restoreSources,
            new[] { this.PackageId },
            regex,
            this.PackageIdReplace,
            previousVersion,
            projectCommmits,
            headCommits,
            this.ReferenceCommit,
            referenceVersions,
            this.NoCache,
            this.DirectDownload,
            this.semanticVersionIncrement,
            GetVersionSuffix,
            new MSBuildNuGetLogger(this.Log)).Result;

        var version = packageId.Version;
        this.ComputedVersion = version.ToString();
        this.ComputedVersionPrefix = version.ToString("x.y.z", NuGet.Versioning.VersionFormatter.Instance);
        this.ComputedVersionSuffix = version.ToString("R", NuGet.Versioning.VersionFormatter.Instance);

        return this.ComputedVersion is not null;

        string? GetVersionSuffix(string? previousVersionRelease = default)
        {
            return this.NoVersionSuffix ? string.Empty : (versionSuffix ?? previousVersionRelease);
        }
    }
}