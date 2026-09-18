namespace TypeSafeAI;

/// <summary>
/// Pairs a question with the answer type it produces, so an answer can be retrieved without a
/// cast and without repeating the question's id as a string.
/// </summary>
/// <typeparam name="TAnswer">The answer type this question produces.</typeparam>
/// <remarks>
/// This interface carries no members. It exists purely so that
/// <see cref="SystemOneResult.Get{TAnswer}(IQuestion{TAnswer})"/> can bind the answer type at
/// compile time:
/// <code>
/// var isUrgent = new NoulQuestion("is_urgent", "Does this convey urgency?");
/// var result = await client.SystemOneAsync(state, [isUrgent], cancellationToken);
/// NoulAnswer answer = result.Get(isUrgent);
/// </code>
/// </remarks>
public interface IQuestion<TAnswer>
    where TAnswer : Answer
{
    /// <summary>
    /// Gets the id this question is keyed by in the request and in the response.
    /// </summary>
    string Id { get; }
}
