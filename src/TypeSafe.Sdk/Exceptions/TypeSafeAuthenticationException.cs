namespace TypeSafe;

/// <summary>
/// The API key was missing, malformed, or rejected (HTTP 401).
/// </summary>
/// <remarks>
/// Check the key itself before looking at the request. A key that is syntactically valid but
/// wrong also arrives here.
/// </remarks>
public sealed class TypeSafeAuthenticationException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeAuthenticationException"/> class.
    /// </summary>
    public TypeSafeAuthenticationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeAuthenticationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeAuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeAuthenticationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
