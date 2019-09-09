// -----------------------------------------------------------------------
// <copyright file="FileNameComparer.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Infrastructure
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
        public int GetHashCode(string obj) => Path.GetFileName(obj).ToLower(System.Globalization.CultureInfo.CurrentCulture).GetHashCode();
    }
}