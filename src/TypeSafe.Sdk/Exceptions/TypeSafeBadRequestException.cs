namespace TypeSafe;

/// <summary>
/// The request was malformed or missing a required field (HTTP 400).
/// </summary>
/// <remarks>
/// Distinct from <see cref="TypeSafeUnprocessableEntityException"/>: 400 means the request
/// could not be parsed, while 422 means it parsed but failed validation.
/// </remarks>
public sealed class TypeSafeBadRequestException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeBadRequestException"/> class.
    /// </summary>
    public TypeSafeBadRequestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeBadRequestException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeBadRequestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeBadRequestException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeBadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
