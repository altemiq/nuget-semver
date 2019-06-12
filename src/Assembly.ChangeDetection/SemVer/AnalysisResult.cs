// -----------------------------------------------------------------------
// <copyright file="AnalysisResult.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.SemVer
{
    /// <summary>
    /// The analysis result.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        public string VersionNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether breaking changes were detected.
        /// </summary>
        public ResultsType ResultsType { get; set; }
    }
}
