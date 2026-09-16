using System.Text.Json.Nodes;

namespace TypeSafe;

/// <summary>
/// Per-call overrides that take precedence over the client's own settings for one request only.
/// </summary>
/// <remarks>
/// Leave a property <see langword="null"/> to inherit the client-level value.
/// </remarks>
public sealed class TypeSafeRequestOptions
{
    /// <summary>
    /// Gets the model to use for this call, overriding the client default.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the retry policy for this call, overriding the client policy.
    /// </summary>
    /// <remarks>
    /// Use <see cref="RetryPolicy.None"/> to disable retrying for a single call.
    /// </remarks>
    public RetryPolicy? Retry { get; init; }

    /// <summary>
    /// Gets the timeout applied to each individual attempt, overriding the client timeout.
    /// </summary>
    /// <remarks>
    /// This bounds one attempt. The whole call is additionally bounded by
    /// <see cref="RetryPolicy.TotalBudget"/>, which covers every attempt and every delay between
    /// them.
    /// </remarks>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets additional request headers for this call, merged over the client's default headers.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets extra top-level request-body fields for this call, merged last so they win on a
    /// collision.
    /// </summary>
    /// <remarks>
    /// The escape hatch for API features this SDK version does not model. Merging is shallow, so an
    /// object value replaces rather than merges into an existing value.
    /// </remarks>
    public IReadOnlyDictionary<string, JsonNode?>? AdditionalBodyProperties { get; init; }
}
