namespace TypeSafeAI;

/// <summary>
/// The request never produced an HTTP response, because the connection could not be established
/// or the response could not be read.
/// </summary>
/// <remarks>
/// The SDK retries connection failures while the retry budget allows. Seeing this type means the
/// retries were exhausted, or retrying was disabled.
/// </remarks>
public class TypeSafeConnectionException : TypeSafeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeConnectionException"/> class.
    /// </summary>
    public TypeSafeConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeConnectionException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeConnectionException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
