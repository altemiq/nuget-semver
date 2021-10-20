// -----------------------------------------------------------------------
// <copyright file="GetProjectCommitsTask.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// The GIT log task.
/// </summary>
public sealed class GetProjectCommitsTask : GitLogTask
{
    /// <summary>
    /// Initialises a new instance of the <see cref="GetProjectCommitsTask"/> class.
    /// </summary>
    public GetProjectCommitsTask() => this.MaxCount = 10;

    /// <summary>
    /// Gets or sets the project dir.
    /// </summary>
    [Required]
    public string ProjectDir { get; set; } = default!;

    /// <summary>
    /// Gets the latest commit.
    /// </summary>
    [Output]
    public string? Commit => this.GitCommits.Select(commit => commit.Sha).FirstOrDefault();

    /// <inheritdoc/>
    protected override ITaskItem? GetPath() => new TaskItem(this.ProjectDir);
}