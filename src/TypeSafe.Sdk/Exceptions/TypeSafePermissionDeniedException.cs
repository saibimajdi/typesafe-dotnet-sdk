namespace TypeSafe;

/// <summary>
/// The API key is valid but is not allowed to perform this request (HTTP 403).
/// </summary>
/// <remarks>
/// Note that the TypeSafe edge proxy can also answer an
/// <em>unauthenticated</em> request with 403 before the application sees it, so a 403 does not
/// guarantee the key was recognised. Inspect the response body to tell the two apart.
/// </remarks>
public sealed class TypeSafePermissionDeniedException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafePermissionDeniedException"/> class.
    /// </summary>
    public TypeSafePermissionDeniedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafePermissionDeniedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafePermissionDeniedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafePermissionDeniedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafePermissionDeniedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
