// -----------------------------------------------------------------------
// <copyright file="GetReferencedProjectsTask.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// Gets the referenced projects.
/// </summary>
public sealed class GetReferencedProjectsTask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets the project path.
    /// </summary>
    [Required]
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets the referenced projects.
    /// </summary>
    [Output]
    public ITaskItem[] ReferencedProjectDirs { get; private set; } = default!;

    /// <inheritdoc/>
    public override bool Execute()
    {
        if (this.ProjectPath is not null && File.Exists(this.ProjectPath))
        {
            this.ReferencedProjectDirs = GetProjects(this.ProjectPath)
                .Select(project => Path.GetDirectoryName(project))
                .Distinct(StringComparer.Ordinal)
                .Select(projectDir => new TaskItem(projectDir))
                .ToArray();
        }
        else
        {
            this.ReferencedProjectDirs = Array.Empty<ITaskItem>();
        }

        return true;
    }

    private static IEnumerable<string> GetProjects(string project)
    {
        var xmlDocument = new System.Xml.XmlDocument();
        using (var xmlReader = System.Xml.XmlReader.Create(File.OpenRead(project), new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore }))
        {
            xmlDocument.Load(xmlReader);
        }

        var projectReferences = xmlDocument.SelectNodes("//ProjectReference");
        if (projectReferences is null)
        {
            yield break;
        }

        var projectDir = Path.GetDirectoryName(project) ?? string.Empty;

        foreach (System.Xml.XmlNode projectReference in projectReferences)
        {
            foreach (var path in GetIncludes(projectReference))
            {
                var evaluatedPath = path
#if NETSTANDARD2_1_OR_GREATER
                    .Replace("\\", "/", StringComparison.Ordinal);
#else
                    .Replace("\\", "/");
#endif
                if (!Path.IsPathRooted(evaluatedPath))
                {
                    evaluatedPath = Path.Combine(projectDir, evaluatedPath);
                }

                evaluatedPath = Path.GetFullPath(evaluatedPath);

                yield return evaluatedPath;

                foreach (var referencedProject in GetProjects(evaluatedPath))
                {
                    yield return referencedProject;
                }
            }
        }

        static IEnumerable<string> GetIncludes(System.Xml.XmlNode node)
        {
            if (node.Attributes is null)
            {
                return Enumerable.Empty<string>();
            }

            if (TryGetIncludes(node.Attributes, out var includes))
            {
                return includes;
            }

            if (TryGetIncludes(node.ChildNodes, out includes))
            {
                return includes;
            }

            return Enumerable.Empty<string>();

            static bool TryGetIncludes(System.Collections.IEnumerable nodes, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IEnumerable<string>? includes)
            {
                const string Include = nameof(Include);
                var node = nodes
                    .OfType<System.Xml.XmlNode>()
                    .FirstOrDefault(node => string.Equals(node.Name, Include, StringComparison.Ordinal));
                if (node?.Value is not null)
                {
                    includes = node.Value.Split(';');
                    return true;
                }

                includes = default;
                return false;
            }
        }
    }
}