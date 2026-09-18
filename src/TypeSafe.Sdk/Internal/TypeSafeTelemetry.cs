using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TypeSafeAI.Internal;

/// <summary>
/// The SDK's <see cref="ActivitySource"/> and <see cref="Meter"/>.
/// </summary>
/// <remarks>
/// <para>
/// The span covers one logical SDK call, <em>including every retry</em>, so a retried request
/// appears as a single span carrying a <c>typesafe.retry.count</c> tag rather than as several
/// unrelated HTTP spans. <c>HttpClient</c>'s own instrumentation is left alone and is not
/// duplicated: subscribing to <c>System.Net.Http</c> still shows the individual attempts nested
/// underneath.
/// </para>
/// <para>
/// The meter reports request counts, retry counts, and token usage. Token counters are genuinely
/// useful here because the request's state and questions share one token budget.
/// </para>
/// </remarks>
internal static class TypeSafeTelemetry
{
    /// <summary>
    /// The name of the SDK's activity source and meter.
    /// </summary>
    public const string SourceName = "TypeSafe.Sdk";

    /// <summary>
    /// The version reported on activities, taken from the assembly.
    /// </summary>
    public static readonly string Version =
        typeof(TypeSafeTelemetry).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    /// <summary>
    /// The activity source to subscribe to for one span per SDK call.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(SourceName, Version);

    /// <summary>
    /// The meter exposing the SDK's counters and histograms.
    /// </summary>
    public static readonly Meter Meter = new(SourceName, Version);

    /// <summary>
    /// Counts completed calls. Tagged with the operation and the outcome.
    /// </summary>
    public static readonly Counter<long> Requests =
        Meter.CreateCounter<long>("typesafe.client.requests", unit: "{request}", description: "Number of TypeSafe API calls made.");

    /// <summary>
    /// Counts retried attempts.
    /// </summary>
    public static readonly Counter<long> Retries =
        Meter.CreateCounter<long>("typesafe.client.retries", unit: "{retry}", description: "Number of TypeSafe API attempts that were retried.");

    /// <summary>
    /// Records input tokens consumed, when the API reports them.
    /// </summary>
    public static readonly Histogram<long> InputTokens =
        Meter.CreateHistogram<long>("typesafe.client.tokens.input", unit: "{token}", description: "Input tokens consumed by TypeSafe API calls.");

    /// <summary>
    /// Records output tokens produced, when the API reports them.
    /// </summary>
    public static readonly Histogram<long> OutputTokens =
        Meter.CreateHistogram<long>("typesafe.client.tokens.output", unit: "{token}", description: "Output tokens produced by TypeSafe API calls.");

    /// <summary>
    /// Records the wall-clock duration of one logical call, including retries.
    /// </summary>
    public static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("typesafe.client.duration", unit: "ms", description: "Duration of TypeSafe API calls, including retries.");
}
