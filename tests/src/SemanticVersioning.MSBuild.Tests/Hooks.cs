// -----------------------------------------------------------------------
// <copyright file="Hooks.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.MSBuild;

internal static class Hooks
{
    [Before(Assembly)]
    public static void Setup(AssemblyHookContext _) => Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
}