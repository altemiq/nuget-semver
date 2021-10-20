// -----------------------------------------------------------------------
// <copyright file="AddedFunctionalityRule.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Rules;

/// <summary>
/// The added functionality rule.
/// </summary>
internal class AddedFunctionalityRule : IRule
{
    /// <inheritdoc/>
    public bool Detect(Diff.AssemblyDiffCollection assemblyDiffCollection) => assemblyDiffCollection.ChangedTypes.Count > 0;
}