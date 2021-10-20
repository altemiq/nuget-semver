// -----------------------------------------------------------------------
// <copyright file="SemanticVersionChange.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The type of change from one Semantic Version to another.
/// </summary>
/// <remarks>
/// <para>
/// This can represent the difference between two specific version numbers. It may also be
/// used to describe the kind of change that is required. (E.g. on comparing a new build of a
/// library with an earlier release, we might conclude that the new version must be at least
/// a <see cref="Minor"/> change from the previous one.
/// </para>
/// </remarks>
public enum SemanticVersionChange
{
    /// <summary>
    /// Identical version numbers, according to Semantic Versioning (i.e. they share the same
    /// Major, Minor, and Patch numbers).
    /// </summary>
    /// <remarks>
    /// <para>
    /// If a package has been released with a particular version number, the Sematic Versioning
    /// 2.0.0 spec requires (https://semver.org/#spec-item-3) that the package not be changed.
    /// Changes must be made only under a newer version number. So for two version numbers to
    /// be the same, one of the following must apply:
    /// </para>
    /// <list type="number">
    /// <item>The two items being compared are the same thing</item>
    /// <item>Nothing has yet been released with the given version number</item>
    /// <item>Semantic Versioning rules have been broken.</item>
    /// </list>
    /// </remarks>
    None,

    /// <summary>
    /// The same Major and Minor version numbers, but a change in patch number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This kind of change indicates no change of intended functionality, and is usually
    /// associated with a bug fix release.
    /// </para>
    /// </remarks>
    Patch,

    /// <summary>
    /// The same Major version number, but a change in Minor version number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This indicates a backwards compatible change: the newer version adds functionality
    /// without removing or changing existing behaviour.
    /// </para>
    /// </remarks>
    Minor,

    /// <summary>
    /// A new Major version number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This generally indicates a breaking change. Either functionality has been removed, or
    /// existing behaviour has been changed in a way that could cause malfunctions in existing
    /// clients of older versions.
    /// </para>
    /// </remarks>
    Major,
}