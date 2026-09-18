namespace TypeSafeAI;

/// <summary>
/// The request body failed server-side validation (HTTP 422).
/// </summary>
/// <remarks>
/// The TypeSafe API uses 422 for a missing required field or a malformed question. The SDK
/// validates the documented rules client-side first, so a 422 usually means the request used a
/// field the SDK does not model, or a rule the SDK does not know about.
/// </remarks>
public sealed class TypeSafeUnprocessableEntityException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeUnprocessableEntityException"/> class.
    /// </summary>
    public TypeSafeUnprocessableEntityException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeUnprocessableEntityException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeUnprocessableEntityException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeUnprocessableEntityException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeUnprocessableEntityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
