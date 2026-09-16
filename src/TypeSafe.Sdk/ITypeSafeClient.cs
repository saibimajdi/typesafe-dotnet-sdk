using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace TypeSafe;

/// <summary>
/// A client for the TypeSafe System One API.
/// </summary>
/// <remarks>
/// Implementations are thread-safe and intended to be created once and reused for the lifetime of
/// the application. A single client comfortably serves many concurrent requests.
/// </remarks>
public interface ITypeSafeClient
{
    /// <summary>
    /// Gets the resource exposing the models available to the account.
    /// </summary>
    IModelsResource Models { get; }

    /// <summary>
    /// Answers named questions about text or structured state.
    /// </summary>
    /// <param name="request">The state, the questions, and any per-call overrides.</param>
    /// <param name="cancellationToken">Cancels the call, including any pending retry.</param>
    /// <returns>The answers, keyed by the question ids that were supplied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The request failed client-side validation.</exception>
    /// <exception cref="TypeSafeApiException">The API returned an unsuccessful response.</exception>
    /// <exception cref="TypeSafeConnectionException">The request never produced a response.</exception>
    /// <exception cref="TypeSafeResponseValidationException">The response could not be read.</exception>
    Task<SystemOneResult> SystemOneAsync(
        SystemOneRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers named questions about a plain-text state.
    /// </summary>
    /// <param name="state">The text to evaluate.</param>
    /// <param name="questions">The questions to ask. Must be non-empty with unique ids.</param>
    /// <param name="cancellationToken">Cancels the call, including any pending retry.</param>
    /// <returns>The answers, keyed by the question ids that were supplied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> or <paramref name="questions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The request failed client-side validation.</exception>
    /// <exception cref="TypeSafeApiException">The API returned an unsuccessful response.</exception>
    /// <exception cref="TypeSafeConnectionException">The request never produced a response.</exception>
    Task<SystemOneResult> SystemOneAsync(
        string state,
        IEnumerable<Question> questions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers named questions about a structured state.
    /// </summary>
    /// <param name="state">The JSON to evaluate: an object, an array, or a string value.</param>
    /// <param name="questions">The questions to ask. Must be non-empty with unique ids.</param>
    /// <param name="cancellationToken">Cancels the call, including any pending retry.</param>
    /// <returns>The answers, keyed by the question ids that were supplied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> or <paramref name="questions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The request failed client-side validation.</exception>
    /// <exception cref="TypeSafeApiException">The API returned an unsuccessful response.</exception>
    /// <exception cref="TypeSafeConnectionException">The request never produced a response.</exception>
    Task<SystemOneResult> SystemOneAsync(
        JsonNode? state,
        IEnumerable<Question> questions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers named questions about a state supplied as an arbitrary object.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="state">The object to serialize as the state.</param>
    /// <param name="questions">The questions to ask. Must be non-empty with unique ids.</param>
    /// <param name="stateTypeInfo">
    /// Source-generated metadata for <typeparamref name="TState"/>. Supplying this keeps the call
    /// safe under trimming and ahead-of-time compilation.
    /// </param>
    /// <param name="cancellationToken">Cancels the call, including any pending retry.</param>
    /// <returns>The answers, keyed by the question ids that were supplied.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The request failed client-side validation.</exception>
    /// <exception cref="TypeSafeApiException">The API returned an unsuccessful response.</exception>
    /// <exception cref="TypeSafeConnectionException">The request never produced a response.</exception>
    Task<SystemOneResult> SystemOneAsync<TState>(
        TState state,
        IEnumerable<Question> questions,
        JsonTypeInfo<TState> stateTypeInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers named questions about a state supplied as an arbitrary object.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="state">The object to serialize as the state.</param>
    /// <param name="questions">The questions to ask. Must be non-empty with unique ids.</param>
    /// <param name="cancellationToken">Cancels the call, including any pending retry.</param>
    /// <returns>The answers, keyed by the question ids that were supplied.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The request failed client-side validation.</exception>
    /// <exception cref="TypeSafeApiException">The API returned an unsuccessful response.</exception>
    /// <exception cref="TypeSafeConnectionException">The request never produced a response.</exception>
    /// <remarks>
    /// The state is serialized with reflection, so this overload is unavailable under trimming or
    /// ahead-of-time compilation. Use the <see cref="JsonTypeInfo{T}"/> overload there. Property
    /// names are used exactly as declared; annotate them with
    /// <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> to rename them.
    /// </remarks>
    [RequiresUnreferencedCode("Serializing the state with reflection is unavailable under trimming. Use the JsonTypeInfo overload instead.")]
    [RequiresDynamicCode("Serializing the state with reflection is unavailable under ahead-of-time compilation. Use the JsonTypeInfo overload instead.")]
    Task<SystemOneResult> SystemOneAsync<TState>(
        TState state,
        IEnumerable<Question> questions,
        CancellationToken cancellationToken = default);
}
