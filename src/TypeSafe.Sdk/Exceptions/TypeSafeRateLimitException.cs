namespace TypeSafe;

/// <summary>
/// The account's rate limit was exceeded (HTTP 429).
/// </summary>
/// <remarks>
/// The SDK already retries 429 responses while the retry budget allows and honours the server's
/// <c>Retry-After</c> / <c>retry-after-ms</c> headers. Seeing this type means the retries were
/// exhausted, so the caller is responsible for backing off for at least <see cref="RetryAfter"/>.
/// </remarks>
public sealed class TypeSafeRateLimitException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeRateLimitException"/> class.
    /// </summary>
    public TypeSafeRateLimitException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeRateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeRateLimitException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeRateLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the delay the server asked the client to wait, or <see langword="null"/> when the
    /// response carried no <c>Retry-After</c> or <c>retry-after-ms</c> header, or carried one
    /// that could not be parsed.
    /// </summary>
    /// <remarks>
    /// Both the delta-seconds and the HTTP-date forms of <c>Retry-After</c> are supported, and
    /// <c>retry-after-ms</c> takes precedence when both are present.
    /// </remarks>
    public TimeSpan? RetryAfter { get; init; }
}
