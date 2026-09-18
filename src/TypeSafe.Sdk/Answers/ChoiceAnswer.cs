using System.Text.Json.Nodes;

namespace TypeSafeAI;

/// <summary>
/// The answer to a <see cref="ChoiceQuestion"/>: the selected option and the full probability
/// distribution across every option.
/// </summary>
/// <remarks>
/// The distribution, not the winning label, is the richer signal. Two options at <c>0.5</c> and
/// <c>0.45</c> mean something very different from <c>0.95</c> and <c>0.03</c>, even though both
/// have the same winner. Read <see cref="Probabilities"/> when that distinction matters, and use
/// <see cref="Confidence"/> when a single number is enough.
/// </remarks>
public sealed class ChoiceAnswer : Answer
{
    private readonly Dictionary<string, double> _probabilities;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChoiceAnswer"/> class.
    /// </summary>
    /// <param name="id">The id of the question this answer responds to.</param>
    /// <param name="label">The highest-probability option.</param>
    /// <param name="probabilities">Every option mapped to its probability.</param>
    /// <param name="confidence">How certain the model is, derived from the distribution.</param>
    /// <param name="additionalProperties">Response fields this SDK version does not model.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="label"/> or <paramref name="probabilities"/> is <see langword="null"/>.
    /// </exception>
    public ChoiceAnswer(
        string id,
        string label,
        IReadOnlyDictionary<string, double> probabilities,
        double confidence,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : base(id, additionalProperties)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(probabilities);

        Label = label;
        Confidence = confidence;
        _probabilities = new Dictionary<string, double>(probabilities, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the selected option: the highest-probability label.
    /// </summary>
    /// <remarks>
    /// This is the wire field <c>choice</c>. It round-trips byte-for-byte from the label supplied
    /// in the question's criteria.
    /// </remarks>
    public string Label { get; }

    /// <summary>
    /// Gets how certain the model is about the answer, from <c>0</c> to <c>1</c>.
    /// </summary>
    /// <remarks>
    /// A flat distribution means low confidence, which usually means no option is a clear winner.
    /// The value is reported by the API and is never recomputed by the SDK.
    /// </remarks>
    public double Confidence { get; }

    /// <summary>
    /// Gets every option mapped to its probability.
    /// </summary>
    /// <remarks>
    /// The API does not guarantee that a requested label appears here, and a missing label is not
    /// the same as a probability of zero. Use <see cref="TryGetProbability"/> when that
    /// distinction matters.
    /// </remarks>
    public IReadOnlyDictionary<string, double> Probabilities => _probabilities;

    /// <inheritdoc />
    public override string Type => "choice";

    /// <summary>
    /// Gets the most probable label, with ties broken ordinally so the result is deterministic.
    /// </summary>
    /// <remarks>
    /// Usually the same as <see cref="Label"/>. They can differ only when the API reports a
    /// distribution whose maximum is not the label it selected, which is worth logging if it
    /// happens.
    /// </remarks>
    public string? TopLabel => Ranked().Select(static pair => pair.Key).FirstOrDefault();

    /// <summary>
    /// Gets the probability of the most probable label, or <c>0</c> when the distribution is empty.
    /// </summary>
    public double TopProbability =>
        _probabilities.Count == 0 ? 0 : _probabilities.Values.Max();

    /// <inheritdoc />
    public override bool TryGetConfidence(out double confidence)
    {
        confidence = Confidence;
        return true;
    }

    /// <summary>
    /// Gets the probability of one option.
    /// </summary>
    /// <param name="label">The option label.</param>
    /// <param name="probability">The option's probability, when present.</param>
    /// <returns><see langword="true"/> when the option is present in the distribution.</returns>
    public bool TryGetProbability(string label, out double probability) =>
        _probabilities.TryGetValue(label, out probability);

    /// <summary>
    /// Gets the probability of one option, or <see langword="null"/> when the option is absent.
    /// </summary>
    /// <param name="label">The option label.</param>
    /// <returns>The option's probability, or <see langword="null"/>.</returns>
    /// <remarks>
    /// Returning <see langword="null"/> rather than <c>0</c> keeps "the API did not report this
    /// option" distinct from "the model ruled this option out".
    /// </remarks>
    public double? ProbabilityOrDefault(string label) =>
        _probabilities.TryGetValue(label, out var probability) ? probability : null;

    /// <summary>
    /// Gets the probability of one option.
    /// </summary>
    /// <param name="label">The option label.</param>
    /// <returns>The option's probability.</returns>
    /// <exception cref="KeyNotFoundException">The option is absent from the distribution.</exception>
    public double ProbabilityOf(string label)
    {
        if (_probabilities.TryGetValue(label, out var probability))
        {
            return probability;
        }

        throw new KeyNotFoundException(
            $"The option '{label}' is not present in the probabilities for answer '{Id}'. The API " +
            "does not guarantee that every requested label is reported; use " +
            $"{nameof(TryGetProbability)} or {nameof(ProbabilityOrDefault)} if the label may be absent.");
    }

    /// <summary>
    /// Enumerates every option from most to least probable.
    /// </summary>
    /// <returns>Options ordered by descending probability, then by label, so the order is stable.</returns>
    public IEnumerable<KeyValuePair<string, double>> Ranked() =>
        _probabilities
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.Ordinal);

    /// <summary>
    /// Enumerates the most probable options.
    /// </summary>
    /// <param name="count">The maximum number of options to return.</param>
    /// <returns>The top options, most probable first.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public IEnumerable<KeyValuePair<string, double>> Top(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Ranked().Take(count);
    }

    /// <summary>
    /// Computes the normalized Shannon entropy of the distribution — the confidence formula
    /// published in the TypeSafe preview migration guide.
    /// </summary>
    /// <returns>A value from <c>0</c> (fully concentrated) to <c>1</c> (fully uniform).</returns>
    /// <remarks>
    /// <para>
    /// This is <em>not</em> the API's <see cref="Confidence"/>, and the SDK never uses it to
    /// populate that member. The current confidence formula is not published, and the documented
    /// values do not match this one. This method exists because callers are explicitly invited to
    /// compute their own statistic from the full distribution.
    /// </para>
    /// <para>
    /// It is the published preview formula, provided for callers who want to reproduce the older
    /// behaviour or who prefer an entropy-based measure.
    /// </para>
    /// </remarks>
    public double NormalizedEntropy() => ProbabilityMath.NormalizedEntropy(_probabilities.Values);

    /// <inheritdoc />
    public override string ToString() => $"{Type}:{Id}={Label}";
}
