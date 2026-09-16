namespace TypeSafe;

/// <summary>
/// The requested endpoint or resource does not exist (HTTP 404).
/// </summary>
/// <remarks>
/// A 404 usually means a wrong <c>BaseUrl</c>, for example a base URL that already includes a
/// path segment the SDK then duplicates.
/// </remarks>
public sealed class TypeSafeNotFoundException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeNotFoundException"/> class.
    /// </summary>
    public TypeSafeNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
