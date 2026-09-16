namespace TypeSafe;

/// <summary>
/// The client is not configured well enough to make a request, for example because no API key
/// was supplied and <c>TYPESAFE_API_KEY</c> is not set.
/// </summary>
/// <remarks>
/// This is always a programming or configuration error and is raised before any network call,
/// so it is never retried.
/// </remarks>
public sealed class TypeSafeConfigurationException : TypeSafeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeConfigurationException"/> class.
    /// </summary>
    public TypeSafeConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
