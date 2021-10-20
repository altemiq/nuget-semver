// -----------------------------------------------------------------------
// <copyright file="BreakingChangeRule.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Rules;

using Altemiq.Assembly.ChangeDetection.Diff;

/// <summary>
/// Rules for breaking changes.
/// </summary>
internal class BreakingChangeRule : IRule
{
    /// <inheritdoc/>
    public bool Detect(AssemblyDiffCollection assemblyDiffCollection)
    {
        if (assemblyDiffCollection.AddedRemovedTypes.RemovedCount > 0)
        {
            return true;
        }

        if (assemblyDiffCollection.ChangedTypes.Count > 0)
        {
            foreach (var typeChange in assemblyDiffCollection.ChangedTypes)
            {
                if (typeChange.HasChangedBaseType)
                {
                    return true;
                }

                if (typeChange.Interfaces.Count > 0 && typeChange.Interfaces.Removed.Any())
                {
                    return true;
                }

                if (typeChange.Events.Removed.Any())
                {
                    return true;
                }

                if (typeChange.Fields.Removed.Any())
                {
                    return true;
                }

                if (typeChange.Methods.Removed.Any())
                {
                    return true;
                }
            }
        }

        return false;
    }
}