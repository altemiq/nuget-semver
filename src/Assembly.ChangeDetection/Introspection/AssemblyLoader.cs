// -----------------------------------------------------------------------
// <copyright file="AssemblyLoader.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Introspection
{
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is ensure that the application does not crash")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1075:AvoidEmptyCatchClauseThatCatchesSystemException", Justification = "This is ensure that the application does not crash")]
        public static AssemblyDefinition LoadCecilAssembly(string fileName, bool immediateLoad = false, bool? readSymbols = null)
        {
            var pdbPath = Path.ChangeExtension(fileName, "pdb");
            var tryReadSymbols = readSymbols ?? File.Exists(pdbPath);
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Length == 0)
            {
                // TODO: t.Info("File {0} has zero byte length", fileName);
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
                    // TODO: t.Info("File {0} is a managed C++ assembly", fileName);
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
                // TODO: t.Info("File {0} is a managed C++ assembly", fileName);
            }
            catch (NullReferenceException)
            {
                // ignore managed c++ targets
                // TODO: t.Info("File {0} is a managed C++ assembly", fileName);
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: t.Info("File {0} is a managed C++ assembly", fileName);
            }
            catch (Exception)
            {
                // TODO: t.Error(Level.L1, "Could not read assembly {0}: {1}", fileName, ex);
            }

            return null;
        }

        private static bool IsManagedCppAssembly(AssemblyDefinition assembly) => assembly.Modules.SelectMany(mod => mod.AssemblyReferences).Any(assemblyRef => assemblyRef.Name == "Microsoft.VisualC");
    }
}