// -----------------------------------------------------------------------
// <copyright file="AddedFunctionalityRule.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.Assembly.ChangeDetection.Rules;

/// <summary>
/// The added functionality rule.
/// </summary>
internal class AddedFunctionalityRule : IRule
{
    /// <inheritdoc/>
    public bool Detect(Diff.AssemblyDiffCollection assemblyDiffCollection) => assemblyDiffCollection.ChangedTypes.Count > 0;
}