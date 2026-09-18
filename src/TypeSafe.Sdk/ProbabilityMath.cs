namespace TypeSafeAI;

/// <summary>
/// Statistics over a probability distribution, offered so callers do not have to re-implement
/// them and so the SDK's definition is documented in one place.
/// </summary>
public static class ProbabilityMath
{
    /// <summary>
    /// Computes the normalized Shannon entropy of a probability distribution — the confidence
    /// formula published in the TypeSafe preview migration guide.
    /// </summary>
    /// <param name="probabilities">The distribution. Values are expected to be non-negative.</param>
    /// <returns>
    /// A value from <c>0</c> (fully concentrated on one outcome) to <c>1</c> (fully uniform), or
    /// <c>0</c> when the distribution has fewer than two outcomes.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The published form is <c>1 - H / log(n)</c>, where <c>H</c> is the Shannon entropy of the
    /// distribution and <c>n</c> is the number of outcomes. The result is expressed as a
    /// "confidence", so a concentrated distribution scores near <c>1</c>.
    /// </para>
    /// <para>
    /// This is <em>not</em> the API's <c>confidence</c> field. The current formula is not
    /// published and produces different values, so never substitute this for a reported
    /// confidence. It is offered because the documentation explicitly invites callers to compute
    /// their own statistic from the full distribution.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="probabilities"/> is <see langword="null"/>.</exception>
    public static double NormalizedEntropy(IEnumerable<double> probabilities)
    {
        ArgumentNullException.ThrowIfNull(probabilities);

        var entropy = 0.0;
        var count = 0;

        foreach (var probability in probabilities)
        {
            count++;

            if (probability > 0)
            {
                entropy -= probability * Math.Log(probability);
            }
        }

        if (count < 2)
        {
            return 0;
        }

        var maximumEntropy = Math.Log(count);

        return maximumEntropy <= 0 ? 0 : Math.Clamp(1 - (entropy / maximumEntropy), 0, 1);
    }

    /// <summary>
    /// Computes the expected value of an indexed distribution, which is what a Score answer's
    /// <see cref="ScoreAnswer.Score"/> already reports.
    /// </summary>
    /// <param name="probabilities">The distribution, keyed by level index.</param>
    /// <returns>The probability-weighted level.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="probabilities"/> is <see langword="null"/>.</exception>
    public static double ExpectedLevel(IEnumerable<KeyValuePair<int, double>> probabilities)
    {
        ArgumentNullException.ThrowIfNull(probabilities);

        var expected = 0.0;
        foreach (var (level, probability) in probabilities)
        {
            expected += level * probability;
        }

        return expected;
    }

    /// <summary>
    /// Computes the variance of an indexed distribution.
    /// </summary>
    /// <param name="probabilities">The distribution, keyed by level index.</param>
    /// <returns>The probability-weighted variance around <see cref="ExpectedLevel"/>.</returns>
    /// <remarks>
    /// A wide spread means the rubric levels overlap or the state does not contain enough to
    /// decide between them, even when <see cref="ExpectedLevel"/> looks decisive.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="probabilities"/> is <see langword="null"/>.</exception>
    public static double Variance(IEnumerable<KeyValuePair<int, double>> probabilities)
    {
        ArgumentNullException.ThrowIfNull(probabilities);

        var materialized = probabilities as IReadOnlyCollection<KeyValuePair<int, double>>
            ?? [.. probabilities];

        var mean = ExpectedLevel(materialized);
        var variance = 0.0;

        foreach (var (level, probability) in materialized)
        {
            var delta = level - mean;
            variance += delta * delta * probability;
        }

        return variance;
    }
}
