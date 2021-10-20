// -----------------------------------------------------------------------
// <copyright file="AssemblyLoader.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Introspection;

using System;
using System.IO;
using System.Linq;
using Mono.Cecil;

/// <summary>
/// The assembly loader.
/// </summary>
internal static class AssemblyLoader
{
    /// <summary>
    /// Loads the Cecil assembly.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="immediateLoad">Set to <see langword="true"/> to immediately load.</param>
    /// <param name="readSymbols">Whether to read the symbols.</param>
    /// <returns>The assembly definition.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1075:AvoidEmptyCatchClauseThatCatchesSystemException", Justification = "This is ensure that the application does not crash")]
    public static AssemblyDefinition? LoadCecilAssembly(string fileName, bool immediateLoad = default, bool? readSymbols = default)
    {
        var pdbPath = Path.ChangeExtension(fileName, "pdb");
        var tryReadSymbols = readSymbols ?? File.Exists(pdbPath);
        var fileInfo = new FileInfo(fileName);
        if (fileInfo.Length == 0)
        {
            return null;
        }

        try
        {
            var readingMode = immediateLoad ? ReadingMode.Immediate : ReadingMode.Deferred;
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(fileInfo.Directory.FullName);
            var readerParameters = new ReaderParameters { ReadSymbols = tryReadSymbols, ReadingMode = readingMode, AssemblyResolver = assemblyResolver };
            var assemblyDef = AssemblyDefinition.ReadAssembly(fileName, readerParameters);

            // Managed C++ assemblies are not supported by Mono Cecil
            if (IsManagedCppAssembly(assemblyDef))
            {
                return null;
            }

            return assemblyDef;
        }
        catch (BadImageFormatException)
        {
            // Ignore invalid images
        }
        catch (IndexOutOfRangeException)
        {
            // ignore managed c++ targets
        }
        catch (NullReferenceException)
        {
            // ignore managed c++ targets
        }
        catch (ArgumentOutOfRangeException)
        {
            // ignore managed c++ assemblies
        }
        catch (Exception)
        {
            // failed to read assembly
        }

        return null;
    }

    private static bool IsManagedCppAssembly(AssemblyDefinition assembly) => assembly.Modules.SelectMany(mod => mod.AssemblyReferences).Any(assemblyRef => string.Equals(assemblyRef.Name, "Microsoft.VisualC", StringComparison.Ordinal));
}