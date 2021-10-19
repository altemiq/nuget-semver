// -----------------------------------------------------------------------
// <copyright file="GetLatestCommitTask.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System.Linq;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Gets the latest commit for a set of paths.
    /// </summary>
    public sealed class GetLatestCommitTask : Microsoft.Build.Utilities.Task
    {
        private readonly GetProjectCommitsTask task = new();

        /// <summary>
        /// Gets or sets the project dir.
        /// </summary>
        [Required]
        public ITaskItem[] Paths { get; set; } = default!;

        /// <summary>
        /// Gets the commits.
        /// </summary>
        [Output]
        public string? Commit { get; private set; }

        /// <inheritdoc cref="Microsoft.Build.Utilities.ToolTask.ToolPath" />
        public string ToolPath
        {
            get => this.task.ToolPath;
            set => this.task.ToolPath = value;
        }

        /// <inheritdoc cref="Microsoft.Build.Utilities.ToolTask.ToolExe" />
        public string ToolExe
        {
            get => this.task.ToolExe;
            set => this.task.ToolExe = value;
        }

        /// <inheritdoc/>
        public override bool Execute()
        {
            this.task.BuildEngine = this.BuildEngine;
            GitCommit? commit = default;

            foreach (var path in this.Paths)
            {
                this.task.GitCommits.Clear();
                this.task.ProjectDir = path.ItemSpec;
                if (this.task.Execute())
                {
                    var currentCommit = this.task.GitCommits.FirstOrDefault();
                    if (commit is null || (currentCommit is not null && commit.AuthorDate < currentCommit.AuthorDate))
                    {
                        commit = currentCommit;
                    }
                }
            }

            this.Commit = commit?.Sha;
            return true;
        }
    }
}