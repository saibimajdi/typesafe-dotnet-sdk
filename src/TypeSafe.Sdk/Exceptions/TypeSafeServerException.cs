namespace TypeSafe;

/// <summary>
/// The TypeSafe API failed to process the request, or was overloaded (HTTP 5xx).
/// </summary>
/// <remarks>
/// Includes the documented <c>529 Overloaded</c> status. The SDK already retries retryable
/// 5xx statuses, so seeing this type means the retry budget was exhausted.
/// </remarks>
public sealed class TypeSafeServerException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeServerException"/> class.
    /// </summary>
    public TypeSafeServerException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeServerException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeServerException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeServerException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
