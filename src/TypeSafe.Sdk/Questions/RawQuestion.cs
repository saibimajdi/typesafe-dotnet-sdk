using System.Text.Json.Nodes;

namespace TypeSafe;

/// <summary>
/// A question sent exactly as supplied, for API features this SDK version does not model.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="RawQuestion"/> to send a question kind the SDK does not know about, or to set a
/// field added to the API after this SDK release. Everything except <c>type</c> is passed through
/// verbatim, so the SDK never becomes the reason a new API feature cannot be used.
/// </para>
/// <para>
/// A raw question's answer deserializes to <see cref="UnknownAnswer"/> unless its kind happens to
/// match one the SDK models, in which case the normal typed answer is produced.
/// </para>
/// </remarks>
public sealed class RawQuestion : Question
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawQuestion"/> class.
    /// </summary>
    /// <param name="id">The id this question is keyed by.</param>
    /// <param name="type">The wire value of the <c>type</c> discriminator.</param>
    /// <param name="body">
    /// The complete question object as it should appear on the wire, excluding <c>type</c> and
    /// <c>id</c>, both of which are supplied separately.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="type"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    public RawQuestion(string id, string type, JsonObject body)
        : base(id, instructions: null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(body);

        Type = type;
        Body = body;
    }

    /// <summary>
    /// Gets the wire value of this question's <c>type</c> discriminator.
    /// </summary>
    public override string Type { get; }

    /// <summary>
    /// Gets the raw question body sent on the wire, excluding <c>type</c>.
    /// </summary>
    /// <remarks>
    /// This is a live <see cref="JsonObject"/>: mutating it changes what is sent. That is
    /// deliberate, so callers can adjust a raw question between attempts, but it also makes
    /// <see cref="RawQuestion"/> the one question type that is not immutable.
    /// </remarks>
    public JsonObject Body { get; }
}
