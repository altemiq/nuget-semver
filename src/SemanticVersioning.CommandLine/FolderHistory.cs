// -----------------------------------------------------------------------
// <copyright file="FolderHistory.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

using LibGit2Sharp;

/// <summary>
/// Represents a folder-related log of commits beyond renames.
/// </summary>
internal class FolderHistory : IEnumerable<LogEntry>
{
    private static readonly List<CommitSortStrategies> AllowedSortStrategies = new()
    {
        CommitSortStrategies.Topological,
        CommitSortStrategies.Time,
        CommitSortStrategies.Topological | CommitSortStrategies.Time,
    };

    private static readonly System.Reflection.PropertyInfo PathPropertyInfo = typeof(LogEntry).GetProperty(nameof(LogEntry.Path))!;

    private static readonly System.Reflection.PropertyInfo CommitPropertyInfo = typeof(LogEntry).GetProperty(nameof(LogEntry.Commit))!;

    private readonly Repository repo;

    private readonly string path;

    private readonly CommitFilter queryFilter;

    /// <summary>
    /// Initialises a new instance of the <see cref="FolderHistory"/> class.
    /// The commits will be enumerated in reverse chronological order.
    /// </summary>
    /// <param name="repo">The repository.</param>
    /// <param name="path">The file's path relative to the repository's root.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    internal FolderHistory(Repository repo, string path)
        : this(repo, path, new CommitFilter())
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="FolderHistory"/> class.
    /// The given <see cref="CommitFilter"/> instance specifies the commit
    /// sort strategies and range of commits to be considered.
    /// Only the time (corresponding to <c>--date-order</c>) and topological
    /// (coresponding to <c>--topo-order</c>) sort strategies are supported.
    /// </summary>
    /// <param name="repo">The repository.</param>
    /// <param name="path">The file's path relative to the repository's root.</param>
    /// <param name="queryFilter">The filter to be used in querying the commit log.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    /// <exception cref="ArgumentException">When an unsupported commit sort strategy is specified.</exception>
    internal FolderHistory(Repository repo, string path, CommitFilter queryFilter)
    {
        if (repo is null)
        {
            throw new ArgumentNullException(nameof(repo));
        }

        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (queryFilter is null)
        {
            throw new ArgumentNullException(nameof(queryFilter));
        }

        // Ensure the commit sort strategy makes sense.
        if (!AllowedSortStrategies.Contains(queryFilter.SortBy))
        {
            throw new ArgumentException(
                "Unsupported sort strategy. Only 'Topological', 'Time', or 'Topological | Time' are allowed.",
                nameof(queryFilter));
        }

        this.repo = repo;
        this.path = path;
        this.queryFilter = queryFilter;
    }

    /// <summary>
    /// Gets the <see cref="IEnumerator{LogEntry}"/> that enumerates the <see cref="LogEntry"/> instances representing the file's history, including renames (as in <c>git log --follow</c>).
    /// </summary>
    /// <returns>A <see cref="IEnumerator{LogEntry}"/>.</returns>
    public IEnumerator<LogEntry> GetEnumerator() => FullHistory(this.repo, this.path, this.queryFilter).GetEnumerator();

    /// <inheritdoc/>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Gets the relevant commits in which the given file was created, changed, or renamed.
    /// </summary>
    /// <param name="repo">The repository.</param>
    /// <param name="path">The file's path relative to the repository's root.</param>
    /// <param name="filter">The filter to be used in querying the commits log.</param>
    /// <returns>A collection of <see cref="LogEntry"/> instances.</returns>
    private static IEnumerable<LogEntry> FullHistory(IRepository repo, string path, CommitFilter filter)
    {
        var map = new Dictionary<Commit, string>();

        foreach (var currentCommit in repo.Commits.QueryBy(filter))
        {
            if (!map.TryGetValue(currentCommit, out var currentPath))
            {
                currentPath = path;
            }

            var currentTreeEntry = currentCommit.Tree[currentPath];

            if (currentTreeEntry == null)
            {
                yield break;
            }

            var parentCount = currentCommit.Parents.Count();
            if (parentCount == 0)
            {
                yield return CreateLogEntry(currentPath, currentCommit);
            }
            else
            {
                DetermineParentPaths(repo, currentCommit, currentPath, map);

                if (parentCount != 1)
                {
                    continue;
                }

                var parentCommit = currentCommit.Parents.Single();
                var parentPath = map[parentCommit];
                var parentTreeEntry = parentCommit.Tree[parentPath];

                if (parentTreeEntry == null ||
                    parentTreeEntry.Target.Id != currentTreeEntry.Target.Id ||
                    !string.Equals(parentPath, currentPath, StringComparison.Ordinal))
                {
                    yield return CreateLogEntry(currentPath, currentCommit);
                }
            }
        }

        static LogEntry CreateLogEntry(string currentPath, Commit commit)
        {
            var entry = new LogEntry();
            PathPropertyInfo.SetValue(entry, currentPath);
            CommitPropertyInfo.SetValue(entry, commit);
            return entry;
        }
    }

    private static void DetermineParentPaths(IRepository repo, Commit currentCommit, string currentPath, IDictionary<Commit, string> map)
    {
        foreach (var parentCommit in currentCommit.Parents.Where(parentCommit => !map.ContainsKey(parentCommit)))
        {
            map.Add(parentCommit, ParentPath(repo, currentCommit, currentPath, parentCommit));
        }
    }

    private static string ParentPath(IRepository repo, Commit currentCommit, string currentPath, Commit parentCommit)
    {
        using var treeChanges = repo.Diff.Compare<TreeChanges>(parentCommit.Tree, currentCommit.Tree);
        var treeEntryChanges = treeChanges.FirstOrDefault(c => string.Equals(c.Path, currentPath, StringComparison.Ordinal));
        return treeEntryChanges?.Status == ChangeKind.Renamed ? treeEntryChanges.OldPath : currentPath;
    }
}