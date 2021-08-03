// -----------------------------------------------------------------------
// <copyright file="GetProjectCommits.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// The GIT log task.
    /// </summary>
    public class GetProjectCommits : GitLogTask
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
        public string? Commit => this.GitCommits.Select(commit => commit.Sha).FirstOrDefault();

        /// <inheritdoc/>
        protected override ITaskItem? GetPath() => new TaskItem(this.ProjectDir);
    }
}