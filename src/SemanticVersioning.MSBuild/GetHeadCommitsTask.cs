// -----------------------------------------------------------------------
// <copyright file="GetHeadCommitsTask.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    /// <summary>
    /// Gets the head commits after the project commits.
    /// </summary>
    public sealed class GetHeadCommitsTask : GitLogTask
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="GetHeadCommitsTask"/> class.
        /// </summary>
        public GetHeadCommitsTask() => this.MaxCount = 25;

        /// <summary>
        /// Gets or sets the project dir.
        /// </summary>
        [Microsoft.Build.Framework.Required]
        public string ProjectDir { get; set; } = default!;

        /// <summary>
        /// Gets or sets the project commit.
        /// </summary>
        public string? ProjectCommit { get; set; }

        /// <inheritdoc/>
        public override bool Execute()
        {
            if (base.Execute())
            {
                if (this.ProjectCommit is not null)
                {
                    var projectCommit = this.ProjectCommit;

                    var index = -1;
                    for (int i = 0; i < this.GitCommits.Count; i++)
                    {
                        if (string.Equals(this.GitCommits[i].Sha, projectCommit, System.StringComparison.Ordinal))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        while (this.GitCommits.Count > index)
                        {
                            this.GitCommits.RemoveAt(index);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override string? GetWorkingDirectory() => GetBaseDirectory(this.ProjectDir);

        /// <inheritdoc/>
        protected override Microsoft.Build.Framework.ITaskItem? GetPath() => default;
    }
}