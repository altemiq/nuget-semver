// -----------------------------------------------------------------------
// <copyright file="AnalysisResult.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.SemVer;

/// <summary>
/// The analysis result.
/// </summary>
public class AnalysisResult
{
    /// <summary>
    /// Initialises a new instance of the <see cref="AnalysisResult"/> class.
    /// </summary>
    /// <param name="versionNumber">The version number.</param>
    /// <param name="resultsType">The results type.</param>
    /// <param name="differences">The differences.</param>
    internal AnalysisResult(
        string? versionNumber,
        ResultsType resultsType,
        Diff.AssemblyDiffCollection differences)
    {
        this.VersionNumber = versionNumber;
        this.ResultsType = resultsType;
        this.Differences = differences;
    }

    /// <summary>
    /// Gets the version number.
    /// </summary>
    public string? VersionNumber { get; }

    /// <summary>
    /// Gets a value indicating whether breaking changes were detected.
    /// </summary>
    public ResultsType ResultsType { get; }

    /// <summary>
    /// Gets the differences.
    /// </summary>
    public Diff.AssemblyDiffCollection Differences { get; }
}