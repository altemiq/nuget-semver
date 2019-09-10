// -----------------------------------------------------------------------
// <copyright file="VersionUtils.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning.TeamCity
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Version utilities.
    /// </summary>
    public static class VersionUtils
    {
        private static readonly Lazy<string> AssemblyVersion =
            new Lazy<string>(() =>
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (assemblyVersionAttribute == null)
                {
                    return assembly.GetName().Version.ToString();
                }
                else
                {
                    return assemblyVersionAttribute.InformationalVersion;
                }
            });

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <returns>The version.</returns>
        public static string GetVersion() => AssemblyVersion.Value;
    }
}
