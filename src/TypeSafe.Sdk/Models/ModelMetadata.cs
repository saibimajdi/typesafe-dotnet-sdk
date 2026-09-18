using System.Text.Json.Serialization;

namespace TypeSafeAI;

/// <summary>
/// A model available to the account.
/// </summary>
/// <param name="Name">The model name to pass as the <c>model</c> of a request.</param>
/// <param name="Description">A human-readable description of the model.</param>
/// <param name="ReleaseDate">The model's release date, as reported by the API.</param>
/// <remarks>
/// <c>jev-latest</c> is an alias rather than a fixed model. A response's
/// <see cref="SystemOneResult.Model"/> reports the concrete version an alias resolved to, which can
/// change over time; pin a specific name from this list when a decision must stay reproducible.
/// </remarks>
public sealed record ModelMetadata(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("release_date")] string ReleaseDate);
