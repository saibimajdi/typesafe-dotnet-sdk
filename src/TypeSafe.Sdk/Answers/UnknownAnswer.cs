using System.Text.Json;
using System.Text.Json.Nodes;

namespace TypeSafe;

/// <summary>
/// An answer whose kind this SDK version does not model.
/// </summary>
/// <remarks>
/// <para>
/// The TypeSafe API can introduce new answer kinds at any time. Rather than failing the whole
/// response, the SDK deserializes an unrecognised answer into this type, logs a warning, and keeps
/// the complete body in <see cref="Raw"/>. Callers that understand the new kind can read it
/// directly, and callers that do not can ignore it.
/// </para>
/// <para>
/// Every other answer in the same response is unaffected, so adopting a new API feature never
/// breaks an existing one.
/// </para>
/// </remarks>
public sealed class UnknownAnswer : Answer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownAnswer"/> class.
    /// </summary>
    /// <param name="id">The id of the question this answer responds to.</param>
    /// <param name="type">The unrecognised <c>type</c> discriminator.</param>
    /// <param name="raw">The complete answer object, exactly as it arrived.</param>
    /// <param name="additionalProperties">Response fields this SDK version does not model.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> or <paramref name="raw"/> is <see langword="null"/>.
    /// </exception>
    public UnknownAnswer(
        string id,
        string type,
        JsonElement raw,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : base(id, additionalProperties)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type = type;
        Raw = raw;
    }

    /// <summary>
    /// Gets the unrecognised <c>type</c> discriminator.
    /// </summary>
    public override string Type { get; }

    /// <summary>
    /// Gets the complete answer object exactly as it arrived, so nothing is lost.
    /// </summary>
    public JsonElement Raw { get; }

    /// <inheritdoc />
    /// <returns>Always <see langword="false"/>; an unmodelled answer kind has no known confidence.</returns>
    public override bool TryGetConfidence(out double confidence)
    {
        confidence = 0;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Type}:{Id} (unknown answer kind)";
}
