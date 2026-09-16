namespace TypeSafe;

/// <summary>
/// The API returned a successful HTTP response whose body was missing or structurally invalid.
/// </summary>
/// <remarks>
/// A response that merely carries fields this SDK version does not model is <em>not</em> an
/// error: unmodelled fields are preserved, and an unrecognised answer kind deserializes to
/// <see cref="UnknownAnswer"/>. This exception means the response did not satisfy the documented
/// contract at all, which usually indicates a proxy, a captive portal, or an incompatible API
/// version rather than a transient fault.
/// </remarks>
public sealed class TypeSafeResponseValidationException : TypeSafeApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeResponseValidationException"/> class.
    /// </summary>
    public TypeSafeResponseValidationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeResponseValidationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TypeSafeResponseValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeResponseValidationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public TypeSafeResponseValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets the JSON path of the offending field, such as <c>answers.tone.confidence</c>, or
    /// <see langword="null"/> when the problem was not attributable to a single field.
    /// </summary>
    public string? FieldPath { get; internal set; }
}
