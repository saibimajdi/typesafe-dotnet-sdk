namespace TypeSafeAI;

/// <summary>
/// Combines several <see cref="ScoreAnswer"/> values into one weighted judgment.
/// </summary>
/// <remarks>
/// <para>
/// This is the composite scoring pattern. A judgment that depends on several things is best split
/// into one Score per thing, with the answers combined in code using weights that express relative
/// importance. When the combined result does not match what your team would decide, change the
/// weights in code and run again — no prompt changes, and no re-evaluation of the questions whose
/// answers you already have.
/// </para>
/// <para>
/// Each score is normalised by its own rubric length first, so rubrics of different lengths
/// combine meaningfully.
/// </para>
/// </remarks>
public static class CompositeScore
{
    /// <summary>
    /// Computes the weighted mean of several normalized Score answers.
    /// </summary>
    /// <param name="parts">The score answers and their weights.</param>
    /// <returns>The weighted mean, from <c>0</c> to <c>1</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parts"/> is empty, every weight is zero, or a weight is negative.</exception>
    public static double Weighted(params WeightedScore[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return Weighted((IEnumerable<WeightedScore>)parts);
    }

    /// <summary>
    /// Computes the weighted mean of several normalized Score answers.
    /// </summary>
    /// <param name="parts">The score answers and their weights.</param>
    /// <returns>The weighted mean, from <c>0</c> to <c>1</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parts"/> is empty, every weight is zero, or a weight is negative.</exception>
    public static double Weighted(IEnumerable<WeightedScore> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var total = 0.0;
        var weightSum = 0.0;

        foreach (var (answer, weight) in parts)
        {
            ArgumentNullException.ThrowIfNull(answer, nameof(parts));

            if (weight < 0)
            {
                throw new ArgumentException(
                    "Composite score weights cannot be negative. A factor that should reduce the " +
                    "result is better expressed by reversing its rubric than by a negative weight.",
                    nameof(parts));
            }

            total += answer.NormalizedScore * weight;
            weightSum += weight;
        }

        if (weightSum <= 0)
        {
            throw new ArgumentException(
                "Composite scoring requires at least one part with a weight greater than zero.",
                nameof(parts));
        }

        return total / weightSum;
    }

    /// <summary>
    /// Computes several named composite scores from one set of answers, for the case where
    /// different roles judge the same factors differently.
    /// </summary>
    /// <param name="parts">The score answers and their weights.</param>
    /// <param name="profiles">
    /// Each profile maps a factor name to its weight. Factors are matched to
    /// <paramref name="parts"/> by the answer's <see cref="Answer.Id"/>.
    /// </param>
    /// <returns>Each profile name mapped to its weighted mean.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="parts"/> or <paramref name="profiles"/> is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyDictionary<string, double> Profiles(
        IEnumerable<WeightedScore> parts,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> profiles)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(profiles);

        var byId = new Dictionary<string, ScoreAnswer>(StringComparer.Ordinal);
        foreach (var (answer, _) in parts)
        {
            ArgumentNullException.ThrowIfNull(answer, nameof(parts));
            byId[answer.Id] = answer;
        }

        var results = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var (profileName, weights) in profiles)
        {
            var weighted = new List<WeightedScore>(weights.Count);

            foreach (var (factor, weight) in weights)
            {
                if (byId.TryGetValue(factor, out var answer))
                {
                    weighted.Add(new WeightedScore(answer, weight));
                }
            }

            results[profileName] = Weighted(weighted);
        }

        return results;
    }
}
