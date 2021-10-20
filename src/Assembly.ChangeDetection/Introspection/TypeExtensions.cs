// -----------------------------------------------------------------------
// <copyright file="TypeExtensions.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Introspection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altemiq.Assembly.ChangeDetection.Diff;
using Mono.Cecil;
using Mono.Collections.Generic;

/// <summary>
/// The <see cref="Type"/> extensions.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Gets the type by name.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="typeName">The type name.</param>
    /// <returns>The type diff.</returns>
    public static TypeDiff GetTypeByName(this IEnumerable<TypeDiff> set, string typeName) => set.FirstOrDefault(type => string.CompareOrdinal(type.ToString(), typeName) == 0);

    /// <summary>
    /// Gets the field by name.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="fieldName">The field name.</param>
    /// <returns>The field definition.</returns>
    public static FieldDefinition GetFieldByName(this IEnumerable<FieldDefinition> list, string fieldName) => GetFieldByNameAndType(list, fieldName, fieldType: null);

    /// <summary>
    /// Gets the field by name.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="fieldType">The field type.</param>
    /// <returns>The field definition.</returns>
    public static FieldDefinition GetFieldByNameAndType(this IEnumerable<FieldDefinition> list, string fieldName, string? fieldType) => list
        .FirstOrDefault(field => (string.CompareOrdinal(field.Name, fieldName) == 0)
        && (string.IsNullOrEmpty(fieldType) || (field.FieldType is not null && string.Equals(field.FieldType.FullName, fieldType, StringComparison.Ordinal))));

    /// <summary>
    /// Gets the event by name.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="eventName">The event name.</param>
    /// <returns>The event definition.</returns>
    public static EventDefinition GetEventByName(this IEnumerable<EventDefinition> list, string eventName) => GetEventByNameAndType(list, eventName, default);

    /// <summary>
    /// Gets the event by name.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="type">The type.</param>
    /// <returns>The event definition.</returns>
    public static EventDefinition GetEventByNameAndType(this IEnumerable<EventDefinition> list, string eventName, string? type) => list.FirstOrDefault(ev => string.Equals(ev.Name, eventName, StringComparison.Ordinal) && (type is null || string.Equals(ev.EventType.FullName, type, StringComparison.Ordinal)));

    /// <summary>
    /// Compares two TypeReferences by its Full Name and declaring assembly.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this TypeDefinition first, TypeDefinition second)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        return string.Equals(first.FullName, second.FullName, StringComparison.Ordinal) && first.Scope.IsEqual(second.Scope);
    }

    /// <summary>
    /// Check if two methods are equal with respect to name, return type, visibility and method modifiers.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this MethodDefinition first, MethodDefinition second)
    {
        // check if function name, modifiers and paramters are still equal
        if (first is not null
            && string.Equals(first.Name, second.Name, StringComparison.Ordinal)
            && string.Equals(first.ReturnType.FullName, second.ReturnType.FullName, StringComparison.Ordinal) && first.Parameters.Count == second.Parameters.Count
            && first.IsPrivate == second.IsPrivate
            && first.IsPublic == second.IsPublic
            && first.IsFamily == second.IsFamily
            && first.IsAssembly == second.IsAssembly
            && first.IsFamilyOrAssembly == second.IsFamilyOrAssembly
            && first.IsVirtual == second.IsVirtual
            && first.IsStatic == second.IsStatic
            && first.GenericParameters.Count == second.GenericParameters.Count)
        {
            // Check function parameter types if there has been any change
            for (var i = 0; i < first.Parameters.Count; i++)
            {
                var pa = first.Parameters[i];
                var pb = second.Parameters[i];

                if (!string.Equals(pa.ParameterType.FullName, pb.ParameterType.FullName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if two events are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this EventDefinition first, EventDefinition second) => string.Equals(first.Name, second.Name, StringComparison.Ordinal)
        && string.Equals(first.EventType.FullName, second.EventType.FullName, StringComparison.Ordinal)
        && first.AddMethod.IsEqual(second.AddMethod);

    /// <summary>
    /// Check if two methods are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this MethodReference first, MethodReference second) => first.IsEqual(second, compareGenericParameters: true);

    /// <summary>
    /// Check if two methods are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <param name="compareGenericParameters">Whether to compare generic parameters.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this MethodReference first, MethodReference second, bool compareGenericParameters)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        if (string.Equals(first.Name, second.Name, StringComparison.Ordinal)
            && first.DeclaringType.GetElementType().IsEqual(second.DeclaringType.GetElementType(), compareGenericParameters)
            && first.ReturnType.IsEqual(second.ReturnType, compareGenericParameters)
            && first.Parameters.IsEqual(second.Parameters, compareGenericParameters))
        {
            return !compareGenericParameters || first.GenericParameters.IsEqual(second.GenericParameters);
        }

        return false;
    }

    /// <summary>
    /// Check if two collection of parameters are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this Collection<ParameterDefinition> first, Collection<ParameterDefinition> second) => first.IsEqual(second, compareGenericParameters: true);

    /// <summary>
    /// Check if two collection of parameters are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <param name="compareGenericParameters">Whether to compare generic parameters.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this Collection<ParameterDefinition> first, Collection<ParameterDefinition> second, bool compareGenericParameters)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            var p1 = first[i];
            var p2 = second[i];

            if (!p1.ParameterType.IsEqual(p2.ParameterType, compareGenericParameters))
            {
                return false;
            }

            // There seems to be a bug in mono cecil. MethodReferences do not
            // contain the IsIn/IsOut property data we would need to check if both methods
            // have the same In/Out signature for this parameter.
            if (p1.MetadataToken.RID == p2.MetadataToken.RID
                && ((p1.IsIn != p2.IsIn) || (p1.IsOut != p2.IsOut)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if scopes are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this IMetadataScope first, IMetadataScope second)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        return string.Equals(ExractAssemblyNameFromScope(first), ExractAssemblyNameFromScope(second), StringComparison.Ordinal);
    }

    /// <summary>
    /// Check if two type references are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this TypeReference first, TypeReference second) => first.IsEqual(second, compareGenericParameters: true);

    /// <summary>
    /// Check if two generic instance types are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this GenericInstanceType first, GenericInstanceType second)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        return first.GenericArguments.IsEqual(second.GenericArguments);
    }

    /// <summary>
    /// Check if two generic instance types are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <param name="compareGenericParameters">Whether to compare generic parameters.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this TypeReference first, TypeReference second, bool compareGenericParameters)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        var xDeclaring = first.DeclaringType;
        var yDeclaring = second.DeclaringType;

        xDeclaring.IsNotNull(() => xDeclaring = first.DeclaringType.GetElementType());
        yDeclaring.IsNotNull(() => yDeclaring = second.DeclaringType.GetElementType());

        var lret = default(bool);

        // Generic parameters are passed as placeholder via method reference
        //  newobj instance void class [BaseLibraryV1]BaseLibrary.ApiChanges.PublicGenericClass`1<string>::.ctor(class [System.Core]System.Func`1<!0>)
        if (AreTypeNamesEqual(first.Name, second.Name)
            && string.Equals(first.Namespace, second.Namespace, StringComparison.Ordinal)
            && IsEqual(xDeclaring, yDeclaring, compareGenericParameters)
            && first.Scope.IsEqual(second.Scope))
        {
            if (compareGenericParameters)
            {
                lret = first.GenericParameters.IsEqual(second.GenericParameters);
            }
            else
            {
                lret = true;
            }
        }

        if (lret && first is GenericInstanceType xGen && second is GenericInstanceType yGen)
        {
            return xGen.IsEqual(yGen);
        }

        return lret;
    }

    /// <summary>
    /// Check if two field definitions are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this FieldDefinition first, FieldDefinition second) => first is not null
        && first.IsPublic == second.IsPublic
        && first.IsFamilyOrAssembly == second.IsFamilyOrAssembly
        && first.IsFamily == second.IsFamily
        && first.IsAssembly == second.IsAssembly
        && first.IsPrivate == second.IsPrivate
        && first.IsStatic == second.IsStatic
        && first.HasConstant == second.HasConstant
        && first.IsInitOnly == second.IsInitOnly
        && string.Equals(first.Name, second.Name, StringComparison.Ordinal)
        && string.Equals(first.FieldType.FullName, second.FieldType.FullName, StringComparison.Ordinal);

    /// <summary>
    /// Check if two collections of type references are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this IList<TypeReference> first, IList<TypeReference> second)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            var type1 = first[i];
            var type2 = second[i];
            if (!type1.IsEqual(type2))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if two collections of generic paramters are equal.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <returns><see langword="true"/> if <paramref name="first"/> and <paramref name="second"/> are equal; otherwise <see langword="false"/>.</returns>
    public static bool IsEqual(this IList<GenericParameter> first, IList<GenericParameter> second)
    {
        if (first is null)
        {
            return second is null;
        }

        if (second is null)
        {
            return false;
        }

        if (first.Count != second.Count)
        {
            return false;
        }

        for (var i = 0; i < first.Count; i++)
        {
            var param1 = first[i];
            var param2 = second[i];

            if (!string.Equals(param1.FullName, param2.FullName, StringComparison.Ordinal) && param1.FullName[0] != '!' && param2.FullName[0] != '!')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Perform action if this instance is not null.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="action">The action.</param>
    public static void IsNotNull(this object value, Action action)
    {
        if (value is not null)
        {
            action();
        }
    }

    /// <summary>
    /// Perform function if this instance is not null.
    /// </summary>
    /// <typeparam name="T">The type of return.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="func">The func.</param>
    /// <returns>The value from <paramref name="func"/>.</returns>
    public static T? IsNotNull<T>(this object value, Func<T> func)
        where T : class => value is null ? default : func();

    private static string ExractAssemblyNameFromScope(IMetadataScope x) => x switch
    {
        AssemblyNameDefinition aDef => aDef.Name,
        AssemblyNameReference aRef => aRef.Name,
        ModuleDefinition aMod => aMod.Assembly.Name.Name,
        ModuleReference aModRef => Path.GetFileNameWithoutExtension(aModRef.Name),
        _ => x.Name,
    };

    private static bool AreTypeNamesEqual(string n1, string n2) => string.Equals(n1, n2, StringComparison.Ordinal) || n1[0] == '!' || n2[0] == '!';
}