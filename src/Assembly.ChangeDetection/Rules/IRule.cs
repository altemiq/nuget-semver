// -----------------------------------------------------------------------
// <copyright file="IRule.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.Assembly.ChangeDetection.Rules;

/// <summary>
/// Represents a rule.
/// </summary>
internal interface IRule
{
    /// <summary>
    /// Detected the rule in the specified assembly difference collection.
    /// </summary>
    /// <param name="assemblyDiffCollection">The assembly difference collection.</param>
    /// <returns><see langword="true"/> if the rule was detected; otherwise <see langword="false"/>.</returns>
    bool Detect(Diff.AssemblyDiffCollection assemblyDiffCollection);
}