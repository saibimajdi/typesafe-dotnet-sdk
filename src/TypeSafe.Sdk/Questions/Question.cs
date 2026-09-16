using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TypeSafe.Serialization;

namespace TypeSafe;

/// <summary>
/// A single typed judgment for a System One model to make about a
/// <see href="https://docs.typesafe.ai/concepts/state">state</see>.
/// </summary>
/// <remarks>
/// <para>
/// Ask for one snap judgment per question. A question a knowledgeable person could answer in a
/// second given the right context is a good question; "analyse this and decide what to do" is
/// not, and is a signal to split the task into several questions and combine the answers in code.
/// </para>
/// <para>
/// Every question in a request sees the same state, is evaluated independently, and is answered
/// under the id chosen here. One question's answer is never context for another, so questions can
/// be added or removed without changing the others' results.
/// </para>
/// <para>
/// The <see cref="Id"/> is for your code only. It is not sent to the model and is not used in
/// inference, so write the complete question in <see cref="Instructions"/> even when the id looks
/// self-explanatory.
/// </para>
/// </remarks>
[JsonConverter(typeof(QuestionJsonConverter))]
public abstract class Question
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Question"/> class.
    /// </summary>
    /// <param name="id">
    /// The id this question is keyed by. Must be non-empty. Ids are opaque: any characters are
    /// allowed, they are never normalised or case-folded, and they round-trip verbatim.
    /// </param>
    /// <param name="instructions">
    /// The question to ask about the state, or <see langword="null"/> to send none. See
    /// <see cref="JsonNode"/> for the accepted shapes: a string, an object, an array, or
    /// <see langword="null"/>. A string is converted implicitly, so
    /// <c>new NoulQuestion("id", "Is this urgent?")</c> works directly.
    /// </param>
    /// <param name="additionalProperties">
    /// Extra JSON fields to merge into this question's object, for API features this SDK version
    /// does not model. Merged last, so these win over the SDK's own fields.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or whitespace.</exception>
    protected Question(
        string id,
        JsonNode? instructions,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        Instructions = instructions;
        AdditionalProperties = additionalProperties ?? EmptyProperties;
    }

    private static IReadOnlyDictionary<string, JsonNode?> EmptyProperties { get; } =
        new Dictionary<string, JsonNode?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the id this question is keyed by, and the key its answer arrives under.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the question to ask about the state, or <see langword="null"/> when none was supplied.
    /// </summary>
    /// <remarks>
    /// The HTTP reference marks <c>instructions</c> as required while both sibling SDKs model it
    /// as optional. The SDK therefore sends it whenever it is set and lets the server's
    /// <c>422</c> be the authority on whether it may be omitted.
    /// </remarks>
    public JsonNode? Instructions { get; }

    /// <summary>
    /// Gets extra JSON fields merged into this question's wire object.
    /// </summary>
    public IReadOnlyDictionary<string, JsonNode?> AdditionalProperties { get; }

    /// <summary>
    /// Gets the wire value of this question's <c>type</c> discriminator.
    /// </summary>
    public abstract string Type { get; }

    /// <summary>
    /// Returns a compact description of the question, for diagnostics.
    /// </summary>
    /// <returns>A string such as <c>noul:is_urgent</c>.</returns>
    public override string ToString() => $"{Type}:{Id}";
}
