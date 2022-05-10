// -----------------------------------------------------------------------
// <copyright file="AssemblyHelper.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// The assembly helper.
/// </summary>
internal static class AssemblyHelper
{
    private static readonly string[] Extensions = { ".dll", ".DLL", ".exe", ".EXE" };

    /// <summary>
    /// Resolves assemblies in this assemblies directory.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">the arguments.</param>
    /// <returns>The resolved assembly.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "This is a delegate.")]
    public static System.Reflection.Assembly? ResolveInDirectory(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new System.Reflection.AssemblyName(args.Name);
        var requestingAssembly = args.RequestingAssembly ?? typeof(AssemblyHelper).Assembly;

        // get the path from the requesting assembly
        var directory = Path.GetDirectoryName(requestingAssembly.Location) ?? string.Empty;

        var path = Extensions
            .Select(extension => assemblyName.Name + extension)
            .Select(fileName => Path.Combine(directory, fileName))
            .FirstOrDefault(File.Exists);

        if (path is not null)
        {
#pragma warning disable S3885 // "Assembly.Load" should be used
            return System.Reflection.Assembly.LoadFile(path);
#pragma warning restore S3885 // "Assembly.Load" should be used
        }

        return default;
    }
}