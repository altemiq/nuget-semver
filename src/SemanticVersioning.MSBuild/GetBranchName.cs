// -----------------------------------------------------------------------
// <copyright file="GetBranchName.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    /// <summary>
    /// Get the branch name.
    /// </summary>
    public sealed class GetBranchName : GitTask
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