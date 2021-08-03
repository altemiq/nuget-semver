// -----------------------------------------------------------------------
// <copyright file="GetLatestCommit.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Build.Framework;

    /// <summary>
    /// Gets the latest commit for a set of paths.
    /// </summary>
    public class GetLatestCommit : Microsoft.Build.Utilities.Task
    {
        private readonly GetProjectCommits task = new();

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
            return this.Commit is not null;
        }
    }
}