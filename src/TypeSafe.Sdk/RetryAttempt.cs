namespace TypeSafeAI;

/// <summary>
/// Describes a retry that is about to be attempted.
/// </summary>
/// <param name="Attempt">
/// The one-based number of the attempt that just failed. Attempt <c>1</c> is the initial request.
/// </param>
/// <param name="Delay">The delay the SDK will wait before the next attempt.</param>
/// <param name="Exception">The failure that triggered the retry.</param>
public readonly record struct RetryAttempt(int Attempt, TimeSpan Delay, Exception Exception);
