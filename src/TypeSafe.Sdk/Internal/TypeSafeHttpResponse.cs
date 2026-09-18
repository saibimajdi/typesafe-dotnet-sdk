using System.Net;

namespace TypeSafeAI.Internal;

/// <summary>
/// The raw outcome of one successful HTTP exchange.
/// </summary>
/// <param name="StatusCode">The response status code.</param>
/// <param name="Body">The response body decoded as UTF-8 text, or an empty string.</param>
/// <param name="RequestId">The <c>x-typesafe-request-id</c> header value, when present.</param>
/// <param name="Headers">A snapshot of the response headers.</param>
/// <remarks>
/// The body is buffered rather than streamed so that retry decisions, error parsing, and response
/// deserialization can all happen after the <see cref="HttpResponseMessage"/> has been disposed.
/// That removes an entire class of use-after-dispose bugs at the cost of one buffer, which is
/// acceptable for a JSON API whose responses are small.
/// </remarks>
internal readonly record struct TypeSafeHttpResponse(
    HttpStatusCode StatusCode,
    string Body,
    string? RequestId,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers)
{
    /// <summary>
    /// Gets a value indicating whether the status code is in the 2xx range.
    /// </summary>
    public bool IsSuccess => (int)StatusCode is >= 200 and <= 299;
}
