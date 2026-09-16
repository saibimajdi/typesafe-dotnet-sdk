using System.Text.Json.Nodes;

namespace TypeSafe;

/// <summary>
/// A yes/no question. Its answer is the probability that the answer is yes.
/// </summary>
/// <remarks>
/// <para>
/// Use a noul when the probability itself is the useful signal: does this message report a bug,
/// is the customer requesting a refund, does the resume mention distributed systems.
/// </para>
/// <para>
/// Do not use a noul to measure a position on a spectrum. A noul value of <c>0.5</c> means yes
/// and no are equally likely; it does not mean "medium". Use a <see cref="ScoreQuestion"/> with
/// described levels for that.
/// </para>
/// </remarks>
public sealed class NoulQuestion : Question, IQuestion<NoulAnswer>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoulQuestion"/> class.
    /// </summary>
    /// <param name="id">The id this question is keyed by.</param>
    /// <param name="instructions">The yes/no question to ask, or <see langword="null"/>.</param>
    /// <param name="criteria">Optional descriptions of what yes and no mean.</param>
    /// <param name="additionalProperties">Extra JSON fields for API features this SDK does not model.</param>
    public NoulQuestion(
        string id,
        JsonNode? instructions = null,
        NoulCriteria? criteria = null,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : base(id, instructions, additionalProperties)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Gets the optional descriptions of what yes and no mean, or <see langword="null"/> when none
    /// were supplied.
    /// </summary>
    public NoulCriteria? Criteria { get; }

    /// <inheritdoc />
    public override string Type => "noul";
}
