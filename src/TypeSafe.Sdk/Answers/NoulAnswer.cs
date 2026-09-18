using System.Text.Json.Nodes;

namespace TypeSafeAI;

/// <summary>
/// The answer to a <see cref="NoulQuestion"/>: the probability that the answer is yes.
/// </summary>
/// <remarks>
/// There is deliberately no confidence member. Noul answers do not carry one; the probability
/// itself is the signal. Read it as "near <c>1</c> is a strong yes, near <c>0</c> a strong no,
/// near <c>0.5</c> genuinely uncertain".
/// </remarks>
public sealed class NoulAnswer : Answer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoulAnswer"/> class.
    /// </summary>
    /// <param name="id">The id of the question this answer responds to.</param>
    /// <param name="probability">The probability that the answer is yes, from <c>0</c> to <c>1</c>.</param>
    /// <param name="additionalProperties">Response fields this SDK version does not model.</param>
    public NoulAnswer(
        string id,
        double probability,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : base(id, additionalProperties)
    {
        Probability = probability;
    }

    /// <summary>
    /// Gets the probability that the answer is yes, from <c>0</c> (no) to <c>1</c> (yes).
    /// </summary>
    /// <remarks>
    /// This is the wire field <c>noul</c>. It is a calibrated probability, not a score: a value of
    /// <c>0.5</c> means yes and no are equally likely. Because it is already a probability, it can
    /// be thresholded directly, and several noul probabilities can be averaged to gate an action
    /// on the mean of several independent conditions.
    /// </remarks>
    public double Probability { get; }

    /// <inheritdoc />
    public override string Type => "noul";

    /// <inheritdoc />
    /// <returns>Always <see langword="false"/>; noul answers carry no confidence.</returns>
    public override bool TryGetConfidence(out double confidence)
    {
        confidence = 0;
        return false;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Type}:{Id}={Probability}";
}
