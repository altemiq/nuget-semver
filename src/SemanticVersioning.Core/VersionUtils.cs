// -----------------------------------------------------------------------
// <copyright file="VersionUtils.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

using System;
using System.Reflection;

/// <summary>
/// Version utilities.
/// </summary>
public static class VersionUtils
{
    private static readonly Lazy<string> AssemblyVersion =
        new(() =>
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return assemblyVersionAttribute is null
                ? assembly.GetName().Version?.ToString() ?? string.Empty
                : assemblyVersionAttribute.InformationalVersion;
        });

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The version.</returns>
    public static string GetVersion() => AssemblyVersion.Value;
}