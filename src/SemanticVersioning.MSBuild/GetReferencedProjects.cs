// -----------------------------------------------------------------------
// <copyright file="GetReferencedProjects.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Gets the referenced projects.
    /// </summary>
    public sealed class GetReferencedProjects : Task
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
            if (this.ProjectPath is not null && System.IO.File.Exists(this.ProjectPath))
            {
                this.ReferencedProjectDirs = GetProjects(this.ProjectPath)
                    .Select(project => System.IO.Path.GetDirectoryName(project))
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
            using (var xmlReader = System.Xml.XmlReader.Create(System.IO.File.OpenRead(project), new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore }))
            {
                xmlDocument.Load(xmlReader);
            }

            var projectReferences = xmlDocument.SelectNodes("//ProjectReference");
            if (projectReferences is null)
            {
                yield break;
            }

            var projectDir = System.IO.Path.GetDirectoryName(project);

            foreach (System.Xml.XmlNode projectReference in projectReferences)
            {
                foreach (var path in GetIncludes(projectReference))
                {
                    var evaluatedPath = path;
                    if (!System.IO.Path.IsPathRooted(evaluatedPath))
                    {
                        evaluatedPath = System.IO.Path.Combine(projectDir, evaluatedPath);
                    }

                    evaluatedPath = System.IO.Path.GetFullPath(evaluatedPath);

                    yield return evaluatedPath;

                    foreach (var referencedProject in GetProjects(evaluatedPath))
                    {
                        yield return referencedProject;
                    }
                }
            }

            static IEnumerable<string> GetIncludes(System.Xml.XmlNode node)
            {
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

                    foreach (System.Xml.XmlNode node in nodes)
                    {
                        if (string.Equals(node.Name, Include, StringComparison.Ordinal))
                        {
                            includes = node.Value.Split(';');
                            return true;
                        }
                    }

                    includes = default;
                    return false;
                }
            }
        }
    }
}