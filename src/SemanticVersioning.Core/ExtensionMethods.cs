// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

/// <summary>
/// Extension methods.
/// </summary>
internal static class ExtensionMethods
{
    /// <summary>
    /// Retrieves a wrapper for the specified entry in the zip archive.
    /// </summary>
    /// <param name="archive">The ZIP archive.</param>
    /// <param name="entryName">A path, relative to the root of the archive, that identifies the entry to retrieve.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the comparison.</param>
    /// <returns>A wrapper for the specified entry in the archive; <see langword="null"/> if the entry does not exist in the archive.</returns>
    public static System.IO.Compression.ZipArchiveEntry? GetEntry(this System.IO.Compression.ZipArchive archive, string entryName, StringComparison comparisonType)
    {
        if (comparisonType is StringComparison.Ordinal)
        {
            return archive.GetEntry(entryName);
        }

        return archive.Entries.FirstOrDefault(entry => string.Equals(entry.FullName, entryName, comparisonType));
    }
}