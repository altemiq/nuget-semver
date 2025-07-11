// -----------------------------------------------------------------------
// <copyright file="TypeQuery.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Query;

using Mono.Cecil;

/// <summary>
/// Query object to search inside an assembly for specific types.
/// </summary>
internal class TypeQuery
{
    private static readonly TypeQueryFactory QueryFactory = new();

    private TypeQueryMode typeQueryMode;

    private string? typeNameFilter;

    /// <summary>
    /// Initialises a new instance of the <see cref="TypeQuery"/> class that searches for all types in a specific namespace with a specific name.
    /// </summary>
    /// <param name="namespaceFilter">The namespace filter.</param>
    /// <param name="typeNameFilter">The type name filter.</param>
    public TypeQuery(string? namespaceFilter = default, string? typeNameFilter = default)
        : this(TypeQueryMode.All, namespaceFilter, typeNameFilter)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="TypeQuery"/> class that searches for specifc types in a specific namespace.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <param name="namespaceFilter">The namespace filter.</param>
    /// <param name="typeNameFilter">The type name filter.</param>
    public TypeQuery(TypeQueryMode mode, string? namespaceFilter = default, string? typeNameFilter = default)
    {
        this.typeQueryMode = mode;

        // To search for nothing makes no sense. Seearch for types with any visibility
        if (!this.IsEnabled(TypeQueryMode.Public) && !this.IsEnabled(TypeQueryMode.Internal))
        {
            this.typeQueryMode |= TypeQueryMode.Internal | TypeQueryMode.Public;
        }

        // If no type interface,struct,class is searched enable all by default
        if (!this.IsEnabled(TypeQueryMode.Class) && !this.IsEnabled(TypeQueryMode.Interface) && !this.IsEnabled(TypeQueryMode.ValueType) && !this.IsEnabled(TypeQueryMode.Enum))
        {
            this.typeQueryMode |= TypeQueryMode.Class | TypeQueryMode.Interface | TypeQueryMode.ValueType | TypeQueryMode.Enum;
        }

        this.NamespaceFilter = namespaceFilter;
        this.TypeNameFilter = typeNameFilter;
    }

    /// <summary>
    /// Gets or sets the search mode.
    /// Restrict the returned types to its visibility and or if the are compiler generated types.
    /// </summary>
    public TypeQueryMode SearchMode
    {
        get => this.typeQueryMode;
        set => this.typeQueryMode = CheckSearchMode(value);
    }

    /// <summary>
    /// Gets or sets the namespace filter. The filter string can container wild cards at
    /// the start and end of the namespace filter.
    /// E.g. *Common (ends with) or *Common* (contains) or Common* (starts with)
    /// or Common (exact match) are possible combinations. An null filter
    /// is treated as no filter.
    /// </summary>
    /// <value>The namespace filter.</value>
    public string? NamespaceFilter { get; set; }

    /// <summary>
    /// Gets or sets the type name filter.
    /// The filter string can contain wild cards at the start and end of the filter query.
    /// </summary>
    public string? TypeNameFilter
    {
        get => this.typeNameFilter;
        set
        {
            this.typeNameFilter = value;

            if (value is null)
            {
                return;
            }

            var idx = value.IndexOf('<');
            if (idx != -1)
            {
                var nestingCount = 0;
                var genericParameterCount = 1;
                for (var i = idx; i < value.Length; i++)
                {
                    if (value[i] == '<')
                    {
                        nestingCount++;
                    }

                    if (value[i] == '>')
                    {
                        nestingCount--;
                    }

                    if (value[i] == ',')
                    {
                        genericParameterCount++;
                    }
                }

                this.typeNameFilter = string.Format(Properties.Resources.Culture, "{0}`{1}", value.Substring(0, idx), genericParameterCount);
            }
        }
    }

    /// <summary>
    /// Parse a query string which can contain a list of type queries separated by ;. A type query can be like a
    /// normal class declaration. E.g. "public class *;public interface *" would be a valid type query list.
    /// </summary>
    /// <param name="typeQueryList">Type query list.</param>
    /// <param name="defaultFlags">
    /// Default query flags to use if none are part of the query. A query does not need to specify
    /// visbility falgs.
    /// </param>
    /// <returns>List of parsed queries.</returns>
    /// <exception cref="ArgumentException">When the query was empty or invalid.</exception>
    /// <exception cref="ArgumentNullException">The input string was null.</exception>
    public static IList<TypeQuery> GetQueries(string typeQueryList, TypeQueryMode defaultFlags) => QueryFactory.GetQueries(typeQueryList, defaultFlags);

    /// <summary>
    /// Helper method to get only one specific type by its full qualified type name.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="typeName">The type name.</param>
    /// <returns>The type definition.</returns>
    public static TypeDefinition? GetTypeByName(AssemblyDefinition assembly, string typeName) => new TypeQuery().GetTypes(assembly).FirstOrDefault(type => string.Equals(type.FullName, typeName, StringComparison.Ordinal));

    /// <summary>
    /// Filters the type list.
    /// </summary>
    /// <param name="typeList">The type list.</param>
    /// <returns>The filtered type list.</returns>
    public IEnumerable<TypeDefinition> Filter(IEnumerable<TypeDefinition> typeList) => typeList.Where(this.TypeMatchesFilter);

    /// <summary>
    /// Gets the types matching the current type query.
    /// </summary>
    /// <param name="assembly">The loaded Mono.Cecil assembly.</param>
    /// <returns>list of matching types.</returns>
    public IList<TypeDefinition> GetTypes(AssemblyDefinition assembly)
    {
        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        return assembly.Modules.SelectMany(module => module.Types.Where(this.TypeMatchesFilter)).ToArray();
    }

    private static bool IsEnabled(TypeQueryMode current, TypeQueryMode requested) => (current & requested) == requested;

    private static bool HasCompilerGeneratedAttribute(TypeDefinition typeDef) => typeDef.CustomAttributes?.Any(att => string.Equals(att.Constructor.DeclaringType.Name, "CompilerGeneratedAttribute", StringComparison.Ordinal)) ?? false;

    private static TypeQueryMode CheckSearchMode(TypeQueryMode mode)
    {
        if (!IsEnabled(mode, TypeQueryMode.Internal) && !IsEnabled(mode, TypeQueryMode.Public))
        {
            throw new ArgumentException(Properties.Resources.MustSetInternalPublic, nameof(mode));
        }

        if (!IsEnabled(mode, TypeQueryMode.Interface) && !IsEnabled(mode, TypeQueryMode.Class) && !IsEnabled(mode, TypeQueryMode.ValueType))
        {
            throw new ArgumentException(Properties.Resources.MustSearchForInterfaceClassStruct, nameof(mode));
        }

        return mode;
    }

    private bool TypeMatchesFilter(TypeDefinition typeDef)
    {
        var lret = default(bool);

        if (this.CheckVisbility(typeDef))
        {
            // throw away compiler generated types
            if (this.IsEnabled(TypeQueryMode.NotCompilerGenerated)
                && (typeDef.IsSpecialName || string.Equals(typeDef.Name, "<Module>", StringComparison.Ordinal) || HasCompilerGeneratedAttribute(typeDef)))
            {
                return false;
            }

            // Nested types have no declaring namespace only the not nested declaring type
            // CAN have it.
            var typeNS = typeDef.Namespace;
            var decl = typeDef.DeclaringType;
            while (decl?.Namespace?.Length == 0)
            {
                decl = decl.DeclaringType;
            }

            if (decl?.Namespace is not null)
            {
                typeNS = decl.Namespace;
            }

            if (!Matcher.MatchWithWildcards(this.NamespaceFilter, typeNS, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!Matcher.MatchWithWildcards(this.TypeNameFilter, typeDef.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (this.IsEnabled(TypeQueryMode.Interface) && typeDef.IsInterface)
            {
                lret = true;
            }

            if (this.IsEnabled(TypeQueryMode.Class) && (typeDef.IsClass && !(typeDef.IsInterface || typeDef.IsValueType)))
            {
                lret = true;
            }

            if (this.IsEnabled(TypeQueryMode.ValueType) && typeDef.IsValueType && !typeDef.IsEnum)
            {
                lret = true;
            }

            if (this.IsEnabled(TypeQueryMode.Enum) && typeDef.IsEnum)
            {
                lret = true;
            }
        }

        return lret;
    }

    private bool IsEnabled(TypeQueryMode mode) => IsEnabled(this.typeQueryMode, mode);

    private bool CheckVisbility(TypeDefinition typedef)
    {
        var lret = false;
        if (this.IsEnabled(TypeQueryMode.Public) && typedef.IsPublic)
        {
            lret = true;
        }

        if (this.IsEnabled(TypeQueryMode.Internal) && !typedef.IsPublic)
        {
            lret = true;
        }

        return lret;
    }
}