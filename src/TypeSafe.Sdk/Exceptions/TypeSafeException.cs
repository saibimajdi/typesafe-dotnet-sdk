namespace TypeSafeAI;

/// <summary>
/// Base type for every error raised by the TypeSafe SDK.
/// </summary>
/// <remarks>
/// Catch <see cref="TypeSafeException"/> to handle any SDK failure, or one of its derived
/// types to handle a specific class of failure. Errors that originate from an HTTP response
/// derive from <see cref="TypeSafeApiException"/>; errors raised before or without a response
/// derive directly from this type.
/// </remarks>
public class TypeSafeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeException"/> class.
    /// </summary>
    public TypeSafeException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
