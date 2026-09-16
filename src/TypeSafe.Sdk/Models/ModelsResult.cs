using System.Text.Json;

namespace TypeSafe;

/// <summary>
/// The result of listing the models available to the account.
/// </summary>
public sealed class ModelsResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsResult"/> class.
    /// </summary>
    /// <param name="models">The available models.</param>
    /// <param name="requestId">The value of the <c>x-typesafe-request-id</c> response header.</param>
    /// <param name="rawJson">The complete response body, exactly as it arrived.</param>
    internal ModelsResult(IReadOnlyList<ModelMetadata> models, string? requestId, JsonElement rawJson)
    {
        Models = models;
        RequestId = requestId;
        RawJson = rawJson;
    }

    /// <summary>
    /// Gets the available models.
    /// </summary>
    public IReadOnlyList<ModelMetadata> Models { get; }

    /// <summary>
    /// Gets the value of the <c>x-typesafe-request-id</c> response header, or <see langword="null"/>
    /// when it was absent. Quote it when reporting a problem to TypeSafe.
    /// </summary>
    public string? RequestId { get; }

    /// <summary>
    /// Gets the complete response body, so fields this SDK version does not model remain reachable.
    /// </summary>
    public JsonElement RawJson { get; }
}
