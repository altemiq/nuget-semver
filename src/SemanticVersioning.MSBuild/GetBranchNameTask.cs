// -----------------------------------------------------------------------
// <copyright file="GetBranchNameTask.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    /// <summary>
    /// Get the branch name.
    /// </summary>
    public sealed class GetBranchNameTask : GitTask
    {
        /// <summary>
        /// Gets or sets the project dir.
        /// </summary>
        [Microsoft.Build.Framework.Required]
        public string ProjectDir { get; set; } = default!;

        /// <summary>
        /// Gets the latest commit.
        /// </summary>
        [Microsoft.Build.Framework.Output]
        public string? Branch { get; private set; }

        /// <inheritdoc/>
        protected override string GenerateCommandLineCommands()
        {
            ////System.Diagnostics.Debugger.Launch();
            var builder = new Microsoft.Build.Utilities.CommandLineBuilder();
            builder.AppendSwitch("branch");
            builder.AppendSwitch("--show-current");
            return builder.ToString();
        }

        /// <inheritdoc/>
        protected override string? GetWorkingDirectory() => GetBaseDirectory(this.ProjectDir);

        /// <inheritdoc/>
        protected override void LogEventsFromTextOutput(string singleLine, Microsoft.Build.Framework.MessageImportance messageImportance)
        {
            this.Branch = singleLine;
            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }
    }
}