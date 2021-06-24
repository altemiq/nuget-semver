// -----------------------------------------------------------------------
// <copyright file="AddedFunctionalityRule.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Rules
{
    /// <summary>
    /// The added functionality rule.
    /// </summary>
    internal class AddedFunctionalityRule : IRule
    {
        /// <inheritdoc/>
        public bool Detect(Diff.AssemblyDiffCollection assemblyDiffCollection) => assemblyDiffCollection.ChangedTypes.Count > 0;
    }
}