using System.Collections;

namespace TypeSafeAI;

/// <summary>
/// An ordered, duplicate-rejecting collection of <see cref="Question"/> objects.
/// </summary>
/// <remarks>
/// <para>
/// This is a convenience for assembling questions dynamically. The client accepts any
/// <see cref="IEnumerable{T}"/> of <see cref="Question"/>, so a collection expression or an array
/// works just as well:
/// </para>
/// <code>
/// var questions = new QuestionSet
/// {
///     new NoulQuestion("is_urgent", "Does this convey urgency?"),
///     new ChoiceQuestion("department", "Which team should handle this?", ["billing", "technical"]),
/// };
/// </code>
/// <para>
/// Send every question that shares a state in one request. System One models evaluate the
/// questions in a request in parallel, so adding questions barely changes response time and costs
/// only the tokens for the extra questions. Asking a question that may not be needed is close to
/// free, which is what makes the speculative fan-out pattern practical.
/// </para>
/// </remarks>
public sealed class QuestionSet : IReadOnlyCollection<Question>
{
    private readonly List<Question> _questions = [];
    private readonly Dictionary<string, Question> _byId = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionSet"/> class.
    /// </summary>
    public QuestionSet()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionSet"/> class.
    /// </summary>
    /// <param name="questions">The questions to add, in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="questions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two questions share an id.</exception>
    public QuestionSet(IEnumerable<Question> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);

        foreach (var question in questions)
        {
            Add(question);
        }
    }

    /// <summary>
    /// Gets the number of questions in the set.
    /// </summary>
    public int Count => _questions.Count;

    /// <summary>
    /// Gets the question with the given id.
    /// </summary>
    /// <param name="id">The question id.</param>
    /// <returns>The matching question.</returns>
    /// <exception cref="KeyNotFoundException">No question has that id.</exception>
    public Question this[string id] => _byId[id];

    /// <summary>
    /// Adds a question to the set.
    /// </summary>
    /// <param name="question">The question to add.</param>
    /// <returns>This set, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="question"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A question with the same id is already present.</exception>
    public QuestionSet Add(Question question)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (!_byId.TryAdd(question.Id, question))
        {
            throw new ArgumentException(
                $"The question id '{question.Id}' appears more than once. Ids are the keys of the " +
                "request's questions map and of the response's answers map, so they must be unique " +
                "within a request.",
                nameof(question));
        }

        _questions.Add(question);
        return this;
    }

    /// <summary>
    /// Determines whether the set contains a question with the given id.
    /// </summary>
    /// <param name="id">The question id.</param>
    /// <returns><see langword="true"/> when a question with that id is present.</returns>
    public bool Contains(string id) => _byId.ContainsKey(id);

    /// <summary>
    /// Gets the question with the given id, if present.
    /// </summary>
    /// <param name="id">The question id.</param>
    /// <param name="question">The matching question, when found.</param>
    /// <returns><see langword="true"/> when a question with that id is present.</returns>
    public bool TryGet(string id, out Question? question) => _byId.TryGetValue(id, out question);

    /// <inheritdoc />
    public IEnumerator<Question> GetEnumerator() => _questions.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
