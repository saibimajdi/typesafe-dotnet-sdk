namespace TypeSafe.Internal;
/// <summary>
/// The outcome of one HTTP attempt.
/// </summary>
/// <param name="Response">The response, when the attempt succeeded.</param>
/// <param name="Failure">The SDK exception describing the failure, when it did not.</param>
/// <param name="Retryable">Whether the failure is worth retrying.</param>
internal readonly record struct AttemptOutcome(
    TypeSafeHttpResponse? Response,
    Exception? Failure,
    bool Retryable);
