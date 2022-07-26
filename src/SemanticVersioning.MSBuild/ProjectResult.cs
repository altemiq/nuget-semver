// -----------------------------------------------------------------------
// <copyright file="ProjectResult.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

/// <summary>
/// The project result.
/// </summary>
/// <param name="Version">The version.</param>
/// <param name="Differences">The differences.</param>
public record ProjectResult(NuGet.Versioning.SemanticVersion Version, Endjin.ApiChange.Api.Diff.AssemblyDiffCollection Differences);