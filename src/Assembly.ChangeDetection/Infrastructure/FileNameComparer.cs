// -----------------------------------------------------------------------
// <copyright file="FileNameComparer.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The file name comparer.
    /// </summary>
    internal class FileNameComparer : IEqualityComparer<string>
    {
        /// <inheritdoc/>
        public bool Equals(string x, string y) => string.Equals(Path.GetFileName(x), Path.GetFileName(y), StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public int GetHashCode(string obj) => StringComparer.Ordinal.GetHashCode(Path.GetFileName(obj).ToLower(System.Globalization.CultureInfo.CurrentCulture));
    }
}