using System.Text.Json.Serialization;

namespace TypeSafeAI;

/// <summary>
/// Token usage for one request.
/// </summary>
/// <param name="InputTokens">
/// The number of input tokens the request consumed, or <see langword="null"/> when the API did not
/// report it.
/// </param>
/// <param name="OutputTokens">
/// The number of output tokens the request produced, or <see langword="null"/> when the API did not
/// report it.
/// </param>
/// <remarks>
/// <para>
/// Both counts are nullable because the API does not always report them, and a missing count is
/// not a count of zero. Treat <see langword="null"/> as "unknown" and decide for yourself whether
/// to substitute <c>0</c> when accumulating totals.
/// </para>
/// <para>
/// The request's <c>state</c> and its <c>questions</c> share one token budget of roughly
/// <see cref="TypeSafeDefaults.ApproximateRequestTokenBudget"/> tokens, so these counters are the
/// way to tell how close a batch is to that shared ceiling.
/// </para>
/// </remarks>
public sealed record Usage(
    [property: JsonPropertyName("input_tokens")] int? InputTokens,
    [property: JsonPropertyName("output_tokens")] int? OutputTokens)
{
    /// <summary>
    /// Gets the total tokens reported for the request, or <see langword="null"/> when neither count
    /// was reported.
    /// </summary>
    public int? TotalTokens =>
        InputTokens is null && OutputTokens is null ? null : (InputTokens ?? 0) + (OutputTokens ?? 0);
}
