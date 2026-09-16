namespace TypeSafe;

/// <summary>
/// A single request attempt exceeded its timeout.
/// </summary>
/// <remarks>
/// This covers the per-attempt timeout (<see cref="TypeSafeClientOptions.Timeout"/>, ten seconds
/// by default) and the caller's <see cref="System.Threading.CancellationToken"/> only when the
/// token is not the cause. A cancelled token surfaces as
/// <see cref="System.OperationCanceledException"/> instead, so callers can tell "the server was
/// too slow" apart from "I cancelled this".
/// </remarks>
public sealed class TypeSafeTimeoutException : TypeSafeConnectionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeTimeoutException"/> class.
    /// </summary>
    public TypeSafeTimeoutException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeTimeoutException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the timeout that was in effect for the attempt that timed out.
    /// </summary>
    public TimeSpan Timeout { get; init; }
}
