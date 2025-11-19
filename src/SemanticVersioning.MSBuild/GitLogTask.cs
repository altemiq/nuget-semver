// -----------------------------------------------------------------------
// <copyright file="GitLogTask.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The <c>git log</c> task.
/// </summary>
public abstract class GitLogTask : GitTask
{
    /// <summary>
    /// Gets or sets the max count.
    /// </summary>
    public int MaxCount { get; set; } = 1;

    /// <summary>
    /// Gets the commits.
    /// </summary>
    [Output]
    public string[] Commits { get; private set; } = [];

    /// <summary>
    /// Gets the git commits.
    /// </summary>
    internal IList<GitCommit> GitCommits { get; } = new List<GitCommit>();

    /// <inheritdoc/>
    public override bool Execute()
    {
        if (base.Execute())
        {
            this.UpdateCommits();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override string GenerateCommandLineCommands()
    {
        var builder = new CommandLineBuilder();
        builder.AppendTextUnquoted("log");
        builder.AppendSwitch("--no-show-signature");
        builder.AppendSwitchIfNotNull("--format=", "%H %aI %cI");
        builder.AppendSwitchIfNotNull("--max-count=", this.MaxCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.AppendFileNameIfNotNull(this.GetPath());

        return builder.ToString();
    }

    /// <inheritdoc/>
    protected override string? GetWorkingDirectory() => this.GetPath() is { } taskItem ? GetBaseDirectory(taskItem.ItemSpec) : default;

    /// <summary>
    /// Gets the path to use for the process.
    /// </summary>
    /// <returns>The path.</returns>
    protected abstract ITaskItem? GetPath();

    /// <inheritdoc/>
    protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
    {
        this.GitCommits.Add(GitCommit.Parse(singleLine));
        base.LogEventsFromTextOutput(singleLine, messageImportance);
    }

    /// <summary>
    /// Updates the <see cref="Commits"/> from <see cref="GitCommits"/>.
    /// </summary>
    protected void UpdateCommits() => this.Commits = [.. this.GitCommits.Select(commit => commit.Sha)];
}