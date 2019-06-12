// -----------------------------------------------------------------------
// <copyright file="AddedFunctionalityRule.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
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
