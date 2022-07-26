// -----------------------------------------------------------------------
// <copyright file="SemanticVersionConverter.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

/// <summary>
/// The <see cref="NuGet.Versioning.SemanticVersion"/> <see cref="System.Text.Json.Serialization.JsonConverter"/>.
/// </summary>
public sealed class SemanticVersionConverter : System.Text.Json.Serialization.JsonConverter<NuGet.Versioning.SemanticVersion>
{
    /// <summary>
    /// The instance.
    /// </summary>
    public static readonly System.Text.Json.Serialization.JsonConverter Instance = new SemanticVersionConverter();

    /// <inheritdoc/>
    public override NuGet.Versioning.SemanticVersion Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => NuGet.Versioning.SemanticVersion.Parse(reader.GetString());

    /// <inheritdoc/>
    public override void Write(System.Text.Json.Utf8JsonWriter writer, NuGet.Versioning.SemanticVersion value, System.Text.Json.JsonSerializerOptions options) => writer.WriteStringValue(value.ToFullString());
}