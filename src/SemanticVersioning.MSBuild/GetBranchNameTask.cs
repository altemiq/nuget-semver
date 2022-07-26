// -----------------------------------------------------------------------
// <copyright file="GetBranchNameTask.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

/// <summary>
/// Get the branch name.
/// </summary>
public sealed class GetBranchNameTask : GitTask
{
    /// <summary>
    /// Gets or sets the project dir.
    /// </summary>
    [Required]
    public string ProjectDir { get; set; } = default!;

    /// <summary>
    /// Gets the latest commit.
    /// </summary>
    [Output]
    public string? Branch { get; private set; }

    /// <inheritdoc/>
    protected override string GenerateCommandLineCommands()
    {
        ////System.Diagnostics.Debugger.Launch();
        var builder = new CommandLineBuilder();
        builder.AppendSwitch("branch");
        builder.AppendSwitch("--show-current");
        return builder.ToString();
    }

    /// <inheritdoc/>
    protected override string? GetWorkingDirectory() => GetBaseDirectory(this.ProjectDir);

    /// <inheritdoc/>
    protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
    {
        this.Branch = singleLine;
        base.LogEventsFromTextOutput(singleLine, messageImportance);
    }
}