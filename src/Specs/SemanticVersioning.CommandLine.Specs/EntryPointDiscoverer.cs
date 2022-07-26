namespace Altavec.SemanticVersioning;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class EntryPointDiscoverer
{
    public static MethodInfo FindStaticEntryMethod(Assembly assembly, string entryPointFullTypeName = default)
    {
        if (Microsoft.Build.Locator.MSBuildLocator.CanRegister)
        {
            var finder = new VisualStudioInstanceFinder();
            var instance = finder.GetVisualStudioInstance(default(System.IO.FileInfo));
            Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);
        }

        var candidates = new List<MethodInfo>();

        if (!string.IsNullOrWhiteSpace(entryPointFullTypeName))
        {
            var typeInfo = assembly.GetType(entryPointFullTypeName, false, false)?.GetTypeInfo();
            if (typeInfo is null)
            {
                throw new InvalidProgramException($"Could not find '{entryPointFullTypeName}' specified for Main method. See <StartupObject> project property.");
            }

            FindMainMethodCandidates(typeInfo, candidates);
        }
        else
        {
            foreach (var type in assembly
                .DefinedTypes
                .Where(t => t.IsClass))
            {
                FindMainMethodCandidates(type, candidates);
            }
        }

        if (candidates.Count > 1)
        {
            throw new AmbiguousMatchException($"Ambiguous entry point. Found multiple static functions named '{MainMethodFullName()}'. Could not identify which method is the main entry point for this function.");
        }

        if (candidates.Count == 0)
        {
            throw new InvalidProgramException($"Could not find a static entry point '{MainMethodFullName()}' that accepts option parameters.");
        }

        return candidates[0];

        string MainMethodFullName()
        {
            return string.IsNullOrWhiteSpace(entryPointFullTypeName) ? "Main" : $"{entryPointFullTypeName}.Main";
        }
    }

    private static void FindMainMethodCandidates(TypeInfo type, List<MethodInfo> candidates) => candidates.AddRange(type
        .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(m => string.Equals("Main", m.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals("<Main>", m.Name, StringComparison.OrdinalIgnoreCase))
        .Where(method => method.ReturnType == typeof(void)
                || method.ReturnType == typeof(int)
                || method.ReturnType == typeof(System.Threading.Tasks.Task)
                || method.ReturnType == typeof(System.Threading.Tasks.Task<int>)));
}