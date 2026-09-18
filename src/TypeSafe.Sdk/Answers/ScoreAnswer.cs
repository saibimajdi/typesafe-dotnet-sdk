using System.Text.Json.Nodes;

namespace TypeSafeAI;

/// <summary>
/// The answer to a <see cref="ScoreQuestion"/>: the probability-weighted position along the
/// rubric, the rubric itself, and the distribution across levels.
/// </summary>
/// <remarks>
/// <see cref="Score"/> can land between two levels, so treat it as a real number rather than an
/// index. A score of <c>2.02</c> on a four-level rubric is normal and means the model put most of
/// its probability mass on level 2.
/// </remarks>
public sealed class ScoreAnswer : Answer
{
    private readonly Dictionary<int, double> _probabilities;
    private readonly Dictionary<int, JsonNode?> _legend;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoreAnswer"/> class.
    /// </summary>
    /// <param name="id">The id of the question this answer responds to.</param>
    /// <param name="score">The probability-weighted position along the rubric.</param>
    /// <param name="legend">
    /// Each level index mapped to the level description that was supplied in the question. The
    /// API echoes those descriptions verbatim, so a value may be a string, an object, an array, or
    /// <see langword="null"/>.
    /// </param>
    /// <param name="probabilities">Each level index mapped to its probability.</param>
    /// <param name="confidence">How certain the model is, derived from the distribution.</param>
    /// <param name="additionalProperties">Response fields this SDK version does not model.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="legend"/> or <paramref name="probabilities"/> is <see langword="null"/>.
    /// </exception>
    public ScoreAnswer(
        string id,
        double score,
        IReadOnlyDictionary<int, JsonNode?> legend,
        IReadOnlyDictionary<int, double> probabilities,
        double confidence,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : base(id, additionalProperties)
    {
        ArgumentNullException.ThrowIfNull(legend);
        ArgumentNullException.ThrowIfNull(probabilities);

        Score = score;
        Confidence = confidence;
        _legend = new Dictionary<int, JsonNode?>(legend);
        _probabilities = new Dictionary<int, double>(probabilities);
    }

    /// <summary>
    /// Gets the probability-weighted position along the rubric.
    /// </summary>
    /// <remarks>
    /// This is the wire field <c>score</c>, equal to the sum of each level index multiplied by its
    /// probability. It can fall between levels, so compare it against thresholds rather than
    /// testing it for equality with an index.
    /// </remarks>
    public double Score { get; }

    /// <summary>
    /// Gets how certain the model is about the answer, from <c>0</c> to <c>1</c>.
    /// </summary>
    /// <remarks>
    /// A low confidence usually means the levels overlap, the judgment is multi-dimensional, or
    /// the state does not contain enough to go on.
    /// </remarks>
    public double Confidence { get; }

    /// <summary>
    /// Gets each level index mapped to the level description supplied in the question, echoed back
    /// verbatim.
    /// </summary>
    /// <remarks>
    /// Because the descriptions are echoed verbatim, a value has the same shape that was sent: a
    /// string, an object, an array, or <see langword="null"/>. Use
    /// <see cref="LegendTextAtLevel"/> when the levels are plain strings.
    /// </remarks>
    public IReadOnlyDictionary<int, JsonNode?> Legend => _legend;

    /// <summary>
    /// Gets each level index mapped to its probability.
    /// </summary>
    /// <remarks>
    /// On the wire these keys are JSON strings. The SDK projects them to integers because that is
    /// what they mean, so no parsing is needed at the call site.
    /// </remarks>
    public IReadOnlyDictionary<int, double> Probabilities => _probabilities;

    /// <summary>
    /// Gets the number of levels reported in the distribution.
    /// </summary>
    public int LevelCount => _probabilities.Count;

    /// <summary>
    /// Gets the highest level index reported in the distribution, or <c>0</c> when the
    /// distribution is empty.
    /// </summary>
    /// <remarks>
    /// This is the denominator for <see cref="NormalizedScore"/>. It is derived from the answer
    /// alone, so a composite score never has to look back at the request to normalise an answer.
    /// </remarks>
    public int MaxLevel => _probabilities.Count == 0 ? 0 : _probabilities.Keys.Max();

    /// <summary>
    /// Gets the score rescaled to <c>0</c> through <c>1</c>, or <c>0</c> when the rubric has a
    /// single level.
    /// </summary>
    /// <remarks>
    /// This is the value to weight when combining several Scores into a composite judgment, because
    /// it makes rubrics of different lengths comparable.
    /// </remarks>
    public double NormalizedScore => MaxLevel <= 0 ? 0 : Score / MaxLevel;

    /// <summary>
    /// Gets the expected level implied by the distribution, which normally equals
    /// <see cref="Score"/>.
    /// </summary>
    public double ExpectedLevel => ProbabilityMath.ExpectedLevel(_probabilities);

    /// <summary>
    /// Gets the variance of the distribution around <see cref="ExpectedLevel"/>.
    /// </summary>
    /// <remarks>
    /// A large variance means the probability mass is split across distant levels, which is a
    /// stronger warning sign than a low confidence alone.
    /// </remarks>
    public double Variance => ProbabilityMath.Variance(_probabilities);

    /// <inheritdoc />
    public override string Type => "score";

    /// <inheritdoc />
    public override bool TryGetConfidence(out double confidence)
    {
        confidence = Confidence;
        return true;
    }

    /// <summary>
    /// Gets the probability reported for one level.
    /// </summary>
    /// <param name="level">The zero-based level index.</param>
    /// <param name="probability">The level's probability, when present.</param>
    /// <returns><see langword="true"/> when the level is present in the distribution.</returns>
    public bool TryGetProbability(int level, out double probability) =>
        _probabilities.TryGetValue(level, out probability);

    /// <summary>
    /// Gets the probability reported for one level.
    /// </summary>
    /// <param name="level">The zero-based level index.</param>
    /// <returns>The level's probability.</returns>
    /// <exception cref="KeyNotFoundException">The level is absent from the distribution.</exception>
    public double ProbabilityAtLevel(int level)
    {
        if (_probabilities.TryGetValue(level, out var probability))
        {
            return probability;
        }

        throw new KeyNotFoundException(
            $"Level {level} is not present in the probabilities for answer '{Id}'. Use " +
            $"{nameof(TryGetProbability)} if the level may be absent.");
    }

    /// <summary>
    /// Gets the level description reported for one level.
    /// </summary>
    /// <param name="level">The zero-based level index.</param>
    /// <returns>The level description, or <see langword="null"/> when it was undescribed or absent.</returns>
    public JsonNode? LegendAtLevel(int level) =>
        _legend.TryGetValue(level, out var description) ? description : null;

    /// <summary>
    /// Gets the level description reported for one level, when that description is a plain string.
    /// </summary>
    /// <param name="level">The zero-based level index.</param>
    /// <returns>The level text, or <see langword="null"/> when the level was undescribed, absent, or
    /// described with structured JSON.</returns>
    public string? LegendTextAtLevel(int level) =>
        LegendAtLevel(level) is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    /// <summary>
    /// Computes the normalized Shannon entropy of the distribution — the confidence formula
    /// published in the TypeSafe preview migration guide.
    /// </summary>
    /// <returns>A value from <c>0</c> (fully concentrated) to <c>1</c> (fully uniform).</returns>
    /// <remarks>
    /// This is <em>not</em> the API's <see cref="Confidence"/>. See
    /// <see cref="ProbabilityMath.NormalizedEntropy"/> for the full caveat.
    /// </remarks>
    public double NormalizedEntropy() => ProbabilityMath.NormalizedEntropy(_probabilities.Values);

    /// <inheritdoc />
    public override string ToString() => $"{Type}:{Id}={Score}";
}
