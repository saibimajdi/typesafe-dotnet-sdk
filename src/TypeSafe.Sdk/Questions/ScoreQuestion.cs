using System.Text.Json.Nodes;

namespace TypeSafe;

/// <summary>
/// A question that rates the state against an ordered rubric the caller defines.
/// </summary>
/// <remarks>
/// <para>
/// Score fits when the answer falls on a spectrum and each point on that spectrum can be
/// described: bug severity, customer frustration, skill level. The order of the levels is their
/// numbering, starting at zero.
/// </para>
/// <para>
/// A rubric takes at least two and at most ten levels. Use as many levels as can be described
/// distinctly; three is usually enough. Levels that overlap make the answer hard to interpret and
/// show up as low confidence.
/// </para>
/// <para>
/// The answer's <see cref="ScoreAnswer.Score"/> is the probability-weighted position along the
/// rubric and can land between two levels, so treat it as a real number rather than an index.
/// </para>
/// </remarks>
public sealed class ScoreQuestion : Question, IQuestion<ScoreAnswer>
{
    private readonly JsonNode?[] _criteria;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoreQuestion"/> class.
    /// </summary>
    /// <param name="id">The id this question is keyed by.</param>
    /// <param name="instructions">What the model should rate, or <see langword="null"/>.</param>
    /// <param name="levels">
    /// An ordered list of level descriptions, from the low end of the scale to the high end. Each
    /// level is either a string or any other accepted JSON shape. Supply between
    /// <see cref="TypeSafeDefaults.MinimumScoreLevels"/> and
    /// <see cref="TypeSafeDefaults.MaximumScoreLevels"/> levels inclusive.
    /// </param>
    /// <param name="additionalProperties">Extra JSON fields for API features this SDK does not model.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="levels"/> has fewer than two or more than ten entries.
    /// </exception>
    public ScoreQuestion(
        string id,
        JsonNode? instructions,
        IEnumerable<JsonNode?> levels,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : base(id, instructions, additionalProperties)
    {
        ArgumentNullException.ThrowIfNull(levels);

        var collected = new List<JsonNode?>(TypeSafeDefaults.MaximumScoreLevels);
        foreach (var level in levels)
        {
            if (collected.Count == TypeSafeDefaults.MaximumScoreLevels)
            {
                throw new ArgumentException(
                    $"A Score rubric accepts at most {TypeSafeDefaults.MaximumScoreLevels} levels, " +
                    "and more than that were supplied. A rubric of eleven levels is rejected by " +
                    "the API. Use as many levels as can be described distinctly; three is usually " +
                    "enough.",
                    nameof(levels));
            }

            collected.Add(level);
        }

        if (collected.Count < TypeSafeDefaults.MinimumScoreLevels)
        {
            throw new ArgumentException(
                $"A Score rubric requires at least {TypeSafeDefaults.MinimumScoreLevels} levels, " +
                $"and {collected.Count} were supplied. Use a Noul question if the answer is a " +
                "plain yes or no.",
                nameof(levels));
        }

        _criteria = [.. collected];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScoreQuestion"/> class from plain text levels.
    /// </summary>
    /// <param name="id">The id this question is keyed by.</param>
    /// <param name="instructions">What the model should rate, or <see langword="null"/>.</param>
    /// <param name="levels">An ordered list of level descriptions, from low to high.</param>
    /// <param name="additionalProperties">Extra JSON fields for API features this SDK does not model.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="levels"/> has fewer than two or more than ten entries.
    /// </exception>
    public ScoreQuestion(
        string id,
        JsonNode? instructions,
        IEnumerable<string> levels,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : this(id, instructions, LevelsFromStrings(levels), additionalProperties)
    {
    }

    /// <summary>
    /// Gets the ordered level descriptions, indexed from zero.
    /// </summary>
    /// <remarks>
    /// These are echoed back verbatim on the answer's <see cref="ScoreAnswer.Legend"/>, so
    /// whatever shape is supplied here is the shape seen there.
    /// </remarks>
    public IReadOnlyList<JsonNode?> Criteria => _criteria;

    /// <summary>
    /// Gets the number of levels in the rubric.
    /// </summary>
    public int LevelCount => _criteria.Length;

    /// <inheritdoc />
    public override string Type => "score";

    private static IEnumerable<JsonNode?> LevelsFromStrings(IEnumerable<string> levels)
    {
        ArgumentNullException.ThrowIfNull(levels);

        foreach (var level in levels)
        {
            yield return JsonValue.Create(level);
        }
    }
}
