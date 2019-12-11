// -----------------------------------------------------------------------
// <copyright file="GenericTypeMapper.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Query
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Altemiq.Assembly.ChangeDetection.Introspection;

    /// <summary>
    /// The generic type mapper.
    /// </summary>
    internal static class GenericTypeMapper
    {
        /// <summary>
        /// Transforms the generic type names.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <param name="typeNameTransformer">The transformer.</param>
        /// <returns>The transformed type name.</returns>
        public static string TransformGenericTypeNames(string typeName, Func<string, string> typeNameTransformer)
        {
            if (typeNameTransformer is null)
            {
                throw new ArgumentNullException(nameof(typeNameTransformer));
            }

            if (string.IsNullOrEmpty(typeName))
            {
                return typeName;
            }

            var normalizedName = typeName.Replace(" ", string.Empty);

            var formattedType = normalizedName;

            var root = ParseGenericType(normalizedName);
            if (root != null)
            {
                TransformGeneric(root, typeNameTransformer);

                var sb = new StringBuilder();
                FormatExpandedGeneric(sb, root);
                formattedType = sb.ToString();
            }

            return formattedType;
        }

        /// <summary>
        /// Converts the CLR type names.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The converted type name.</returns>
        public static string ConvertClrTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return typeName;
            }

            var normalizedName = typeName.Replace(" ", string.Empty);

            // No generic type then we need no mapping
            if (typeName.IndexOf('<') == -1)
            {
                return TypeMapper.ShortToFull(typeName);
            }

            var root = ParseGenericType(normalizedName);

            var sb = new StringBuilder();
            FormatExpandedGeneric(sb, root!);
            return sb.ToString();
        }

        private static void TransformGeneric(GenericType type, Func<string, string> typeNameTransformer)
        {
            if (type is null)
            {
                return;
            }

            type.GenericTypeName = typeNameTransformer(type.GenericTypeName);
            foreach (var typeArg in type.Arguments)
            {
                TransformGeneric(typeArg, typeNameTransformer);
            }
        }

        private static GenericType? ParseGenericType(string normalizedName)
        {
            var curArg = new StringBuilder();
            var root = default(GenericType?);
            var curType = default(GenericType?);

            // Func< Func<Func<int,int>,bool> >
            // Func`1< Func`2< Func`2<System.Int32,System.Int32>, System.Boolean> >
            // Func<int,bool,int>
            for (var i = 0; i < normalizedName.Length; i++)
            {
                if (normalizedName[i] == '<')
                {
                    if (curType is null)
                    {
                        curType = new GenericType(curArg.ToString(), null);
                        root = curType;
                    }
                    else
                    {
                        var newGeneric = new GenericType(curArg.ToString(), curType);
                        curType.Arguments.Add(newGeneric);
                        curType = newGeneric;
                    }

                    curArg.Length = 0;
                }
                else if (normalizedName[i] == '>')
                {
                    if (curArg.Length > 0 && curType != null)
                    {
                        curType.Arguments.Add(new GenericType(TypeMapper.ShortToFull(curArg.ToString()), null));
                    }

                    if (curType?.Parent != null)
                    {
                        curType = curType.Parent;
                    }

                    curArg.Length = 0;
                }
                else if (normalizedName[i] == ',')
                {
                    if (curArg.Length > 0 && curType != null)
                    {
                        curType.Arguments.Add(new GenericType(TypeMapper.ShortToFull(curArg.ToString()), null));
                    }

                    curArg.Length = 0;
                }
                else
                {
                    curArg.Append(normalizedName[i]);
                }
            }

            return root;
        }

        private static void FormatExpandedGeneric(StringBuilder sb, GenericType type)
        {
            sb.Append(type.GenericTypeName);
            if (type.Arguments.Count > 0)
            {
                sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "`{0}", type.Arguments.Count);
                sb.Append("<");
                for (var i = 0; i < type.Arguments.Count; i++)
                {
                    var curGen = type.Arguments[i];
                    if (curGen.Arguments.Count > 0)
                    {
                        FormatExpandedGeneric(sb, curGen);
                    }
                    else
                    {
                        sb.Append(curGen.GenericTypeName);
                    }

                    if (i != type.Arguments.Count - 1)
                    {
                        sb.Append(',');
                    }
                }

                sb.Append(">");
            }
        }

        private class GenericType
        {
            public GenericType(string typeName, GenericType? parent)
            {
                this.GenericTypeName = typeName;

                var idx = typeName.IndexOf('`');
                if (idx != -1)
                {
                    this.GenericTypeName = typeName.Substring(0, idx);
                }

                this.Parent = parent;
            }

            public IList<GenericType> Arguments { get; } = new List<GenericType>();

            public GenericType? Parent { get; }

            public string GenericTypeName { get; set; }
        }
    }
}