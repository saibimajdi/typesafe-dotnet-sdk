using System.Text.Json;

namespace TypeSafeAI;

/// <summary>
/// The error information carried by an unsuccessful TypeSafe API response.
/// </summary>
/// <remarks>
/// <para>
/// The API wraps errors in a single <c>detail</c> member, and that member is polymorphic:
/// </para>
/// <list type="bullet">
///   <item><description>
///     Application errors use an object:
///     <c>{"detail":{"error_type":"authentication_error","message":"..."}}</c>.
///   </description></item>
///   <item><description>
///     Framework errors use a bare string: <c>{"detail":"Not Found"}</c>.
///   </description></item>
/// </list>
/// <para>
/// Both shapes are represented here, and a non-JSON or empty body yields an instance whose
/// <see cref="Body"/> is <see langword="null"/> rather than an exception, because failing to
/// parse an error must never mask the error itself.
/// </para>
/// </remarks>
public sealed class TypeSafeErrorDetails
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeErrorDetails"/> class.
    /// </summary>
    /// <param name="errorType">The API's error classification, when it supplied one.</param>
    /// <param name="message">The API's human-readable message, when it supplied one.</param>
    /// <param name="body">The complete, unmodified error body.</param>
    public TypeSafeErrorDetails(string? errorType, string? message, JsonElement? body)
    {
        ErrorType = errorType;
        Message = message;
        Body = body;
    }

    /// <summary>
    /// Gets the API's machine-readable error classification, such as <c>authentication_error</c>.
    /// </summary>
    public string? ErrorType { get; }

    /// <summary>
    /// Gets the API's human-readable message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the complete, unmodified error body.
    /// </summary>
    public JsonElement? Body { get; }
}
