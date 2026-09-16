using System.Text.Json.Nodes;

namespace TypeSafe;

/// <summary>
/// Optional descriptions of what a yes and a no mean for a <see cref="NoulQuestion"/>.
/// </summary>
/// <remarks>
/// The question alone is enough for most noul questions. Supply criteria when the boundary
/// between yes and no is subtle and needs pinning down.
/// </remarks>
public sealed class NoulCriteria
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoulCriteria"/> class.
    /// </summary>
    /// <param name="true">What a yes (a value near <c>1</c>) means, or <see langword="null"/>.</param>
    /// <param name="false">What a no (a value near <c>0</c>) means, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">Both sides are <see langword="null"/>.</exception>
    public NoulCriteria(JsonNode? @true = null, JsonNode? @false = null)
    {
        if (@true is null && @false is null)
        {
            throw new ArgumentException(
                "A noul criteria must describe at least one of the yes and no outcomes. " +
                "Omit the criteria entirely instead of supplying an empty one.",
                nameof(@true));
        }

        True = @true;
        False = @false;
    }

    /// <summary>
    /// Gets the description of the yes outcome, or <see langword="null"/> when it is undescribed.
    /// </summary>
    public JsonNode? True { get; }

    /// <summary>
    /// Gets the description of the no outcome, or <see langword="null"/> when it is undescribed.
    /// </summary>
    public JsonNode? False { get; }
}
