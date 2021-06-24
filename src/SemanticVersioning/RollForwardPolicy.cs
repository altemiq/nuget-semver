// -----------------------------------------------------------------------
// <copyright file="RollForwardPolicy.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    /// <summary>
    /// The roll forward policy.
    /// </summary>
    internal enum RollForwardPolicy
    {
        /// <summary>
        /// The specified policy is not supported.
        /// </summary>
        Unsupported,

        /// <summary>
        /// Doesn't roll forward. Exact match required.
        /// </summary>
        Disable,

        /// <summary>
        /// <para>Uses the specified version.</para>
        /// <para>If not found, rolls forward to the latest patch level.</para>
        /// <para>If not found, fails.</para>
        /// <para>This value is the legacy behavior from the earlier versions of the SDK.</para>
        /// </summary>
        Patch,

        /// <summary>
        /// <para>Uses the latest patch level for the specified major, minor, and feature band.</para>
        /// <para>If not found, rolls forward to the next higher feature band within the same major/minor and uses the latest patch level for that feature band.</para>
        /// <para>If not found, fails.</para>
        /// </summary>
        Feature,

        /// <summary>
        /// <para>Uses the latest patch level for the specified major, minor, and feature band.</para>
        /// <para>If not found, rolls forward to the next higher feature band within the same major/minor version and uses the latest patch level for that feature band.</para>
        /// <para>If not found, rolls forward to the next higher minor and feature band within the same major and uses the latest patch level for that feature band.</para>
        /// <para>If not found, fails.</para>
        /// </summary>
        Minor,

        /// <summary>
        /// <para>Uses the latest patch level for the specified major, minor, and feature band.</para>
        /// <para>If not found, rolls forward to the next higher feature band within the same major/minor version and uses the latest patch level for that feature band.</para>
        /// <para>If not found, rolls forward to the next higher minor and feature band within the same major and uses the latest patch level for that feature band.</para>
        /// <para>If not found, rolls forward to the next higher major, minor, and feature band and uses the latest patch level for that feature band.</para>
        /// <para>If not found, fails.</para>
        /// </summary>
        Major,

        /// <summary>
        /// <para>Uses the latest installed patch level that matches the requested major, minor, and feature band with a patch level and that is greater or equal than the specified value.</para>
        /// <para>If not found, fails.</para>
        /// </summary>
        LatestPatch,

        /// <summary>
        /// <para>Uses the highest installed feature band and patch level that matches the requested major and minor with a feature band and patch level that is greater or equal than the specified value.</para>
        /// <para>If not found, fails.</para>
        /// </summary>
        LatestFeature,

        /// <summary>
        /// <para>Uses the highest installed minor, feature band, and patch level that matches the requested major with a minor, feature band, and patch level that is greater or equal than the specified value.</para>
        /// <para>If not found, fails.</para>
        /// </summary>
        LatestMinor,

        /// <summary>
        /// <para>Uses the highest installed .NET SDK with a version that is greater or equal than the specified value.</para>
        /// <para>If not found, fail.</para>
        /// </summary>
        LatestMajor,
    }
}