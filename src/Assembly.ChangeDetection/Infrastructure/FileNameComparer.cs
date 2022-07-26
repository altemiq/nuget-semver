// -----------------------------------------------------------------------
// <copyright file="FileNameComparer.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.Assembly.ChangeDetection.Infrastructure;

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