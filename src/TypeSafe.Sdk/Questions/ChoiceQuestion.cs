using System.Text.Json.Nodes;

namespace TypeSafe;

/// <summary>
/// A question that selects one option from a set the caller defines.
/// </summary>
/// <remarks>
/// <para>
/// Choice fits when the answer is one of a known set of options with no order between them:
/// routing a ticket to a department, classifying a document, detecting a programming language.
/// </para>
/// <para>
/// Give the full list of options rather than a shortlist: option labels and their descriptions
/// are both sent to the model, so a richer list gives a better answer at a cost of a few tokens
/// each. Add an <c>other</c> or <c>none of the above</c> option whenever the list might not cover
/// every input, so the model has somewhere to put a value that fits none of the others. The SDK
/// never injects such an option for you.
/// </para>
/// <para>
/// A Choice accepts at most 255 options. The documentation notes that a Choice "works reliably up
/// to roughly 240 options", so treat 240 as a practical ceiling.
/// </para>
/// </remarks>
public sealed class ChoiceQuestion : Question, IQuestion<ChoiceAnswer>
{
    private readonly string[] _labels;
    private readonly Dictionary<string, JsonNode?> _criteria;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChoiceQuestion"/> class from bare option labels.
    /// </summary>
    /// <param name="id">The id this question is keyed by.</param>
    /// <param name="instructions">What the model should decide, or <see langword="null"/>.</param>
    /// <param name="labels">
    /// The option labels. Each label is sent to the model as-is and is echoed back in the answer,
    /// so labels round-trip byte-for-byte.
    /// </param>
    /// <param name="additionalProperties">Extra JSON fields for API features this SDK does not model.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="labels"/> is empty, contains more than 255 entries, contains a null or
    /// whitespace label, or contains a duplicate.
    /// </exception>
    public ChoiceQuestion(
        string id,
        JsonNode? instructions,
        IEnumerable<string> labels,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : this(id, instructions, CriteriaFromLabels(labels), additionalProperties)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChoiceQuestion"/> class from a label-to-description map.
    /// </summary>
    /// <param name="id">The id this question is keyed by.</param>
    /// <param name="instructions">What the model should decide, or <see langword="null"/>.</param>
    /// <param name="criteria">
    /// Option labels mapped to a rubric description, or <see langword="null"/> for an option that
    /// needs no extra detail.
    /// </param>
    /// <param name="additionalProperties">Extra JSON fields for API features this SDK does not model.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="criteria"/> is empty, contains more than 255 entries, contains a null or
    /// whitespace label, or contains a duplicate.
    /// </exception>
    public ChoiceQuestion(
        string id,
        JsonNode? instructions,
        IEnumerable<KeyValuePair<string, string?>> criteria,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : this(id, instructions, CriteriaFromStrings(criteria), additionalProperties)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChoiceQuestion"/> class from a label-to-description map
    /// whose descriptions may be arbitrary JSON.
    /// </summary>
    /// <param name="id">The id this question is keyed by.</param>
    /// <param name="instructions">What the model should decide, or <see langword="null"/>.</param>
    /// <param name="criteria">
    /// Option labels mapped to a description of any accepted shape — a string, an object, an
    /// array, or <see langword="null"/>. Structured descriptions are useful for rubrics that
    /// separate options from each other, or for taxonomies the model should walk.
    /// </param>
    /// <param name="additionalProperties">Extra JSON fields for API features this SDK does not model.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="criteria"/> is empty, contains more than 255 entries, contains a null or
    /// whitespace label, or contains a duplicate.
    /// </exception>
    public ChoiceQuestion(
        string id,
        JsonNode? instructions,
        IEnumerable<KeyValuePair<string, JsonNode?>> criteria,
        IReadOnlyDictionary<string, JsonNode?>? additionalProperties = null)
        : base(id, instructions, additionalProperties)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        _labels = new string[255];
        _criteria = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);

        var count = 0;
        foreach (var (label, description) in criteria)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(label, nameof(criteria));

            if (count == TypeSafeDefaults.MaximumChoiceOptions)
            {
                throw new ArgumentException(
                    $"A Choice accepts at most {TypeSafeDefaults.MaximumChoiceOptions} options, and " +
                    $"more than that were supplied. The documentation notes that a Choice works " +
                    "reliably up to roughly 240 options, so consider a Score or a two-stage " +
                    "hierarchical classification instead of one very large Choice.",
                    nameof(criteria));
            }

            if (!_criteria.TryAdd(label, description))
            {
                throw new ArgumentException(
                    $"The option label '{label}' appears more than once. Option labels must be " +
                    "unique because they are the keys of the request's criteria map and the keys " +
                    "of the answer's probabilities map.",
                    nameof(criteria));
            }

            _labels[count++] = label;
        }

        if (count == 0)
        {
            throw new ArgumentException(
                "A Choice requires at least one option.",
                nameof(criteria));
        }

        Array.Resize(ref _labels, count);
    }

    /// <summary>
    /// Gets the option labels, in the order they were supplied.
    /// </summary>
    /// <remarks>
    /// The order is preserved so that a serialized request is deterministic and can be cached or
    /// hashed. The API itself treats the options as an unordered set.
    /// </remarks>
    public IReadOnlyList<string> Labels => _labels;

    /// <summary>
    /// Gets each option label mapped to its rubric description, or <see langword="null"/> for an
    /// option that needs no extra detail.
    /// </summary>
    public IReadOnlyDictionary<string, JsonNode?> Criteria => _criteria;

    /// <inheritdoc />
    public override string Type => "choice";

    private static IEnumerable<KeyValuePair<string, JsonNode?>> CriteriaFromLabels(
        IEnumerable<string> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        foreach (var label in labels)
        {
            yield return new KeyValuePair<string, JsonNode?>(label, null);
        }
    }

    private static IEnumerable<KeyValuePair<string, JsonNode?>> CriteriaFromStrings(
        IEnumerable<KeyValuePair<string, string?>> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        foreach (var (label, description) in criteria)
        {
            yield return new KeyValuePair<string, JsonNode?>(
                label,
                description is null ? null : JsonValue.Create(description));
        }
    }
}
