using System.Text.Json;
using Microsoft.Extensions.Logging;
using TypeSafeAI.Internal;
using TypeSafeAI.Serialization;

namespace TypeSafeAI;

/// <summary>
/// Turns a successful HTTP response into the SDK's model types.
/// </summary>
/// <remarks>
/// The reader is deliberately tolerant. A response that carries extra fields, an unfamiliar answer
/// kind, or a differently shaped <c>usage</c> object still produces a usable result; only a body
/// that does not satisfy the documented contract at all is rejected.
/// </remarks>
internal static class TypeSafeResponseReader
{
    private const string DocumentationUrl = "https://docs.typesafe.ai/api";

    /// <summary>
    /// Reads a System One response.
    /// </summary>
    /// <param name="response">The raw response.</param>
    /// <param name="logger">The logger used to warn about unmodelled answer kinds.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="TypeSafeResponseValidationException">The body is not a valid response.</exception>
    public static SystemOneResult ReadSystemOne(TypeSafeHttpResponse response, ILogger logger)
    {
        using var document = ParseBody(response);

        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw Invalid(response, "The response body was not a JSON object.", fieldPath: null);
        }

        if (!root.TryGetProperty("answers", out var answers) || answers.ValueKind != JsonValueKind.Object)
        {
            // A successful response without an answers map cannot be acted on, and returning an
            // empty result would surface later as a confusing missing-key error.
            throw Invalid(response, "The response body did not contain an 'answers' object.", "answers");
        }

        var result = ReadResult(response, root);

        foreach (var (id, unknown) in result.UnknownAnswers)
        {
            TypeSafeLog.UnknownAnswerKind(logger, id, unknown.Type);
        }

        RecordUsage(result);

        return result;
    }

    /// <summary>
    /// Reads a models response.
    /// </summary>
    /// <param name="response">The raw response.</param>
    /// <returns>The parsed models.</returns>
    /// <exception cref="TypeSafeResponseValidationException">The body was not valid JSON.</exception>
    public static ModelsResult ReadModels(TypeSafeHttpResponse response)
    {
        using var document = ParseBody(response);
        return SystemOneResultJsonConverter.ReadModels(document.RootElement, response.RequestId);
    }

    /// <summary>
    /// Parses the body, converting a malformed payload into the SDK's own exception type.
    /// </summary>
    /// <param name="response">The raw response.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="TypeSafeResponseValidationException">The body was not valid JSON.</exception>
    private static JsonDocument ParseBody(TypeSafeHttpResponse response)
    {
        try
        {
            return JsonDocument.Parse(response.Body);
        }
        catch (JsonException ex)
        {
            throw Invalid(
                response,
                "The TypeSafe API returned a successful response whose body was not valid JSON. " +
                "This usually means a proxy or captive portal answered instead of the API.",
                fieldPath: null,
                ex);
        }
    }

    private static SystemOneResult ReadResult(TypeSafeHttpResponse response, JsonElement root)
    {
        try
        {
            return SystemOneResultJsonConverter.ReadElement(root, response.RequestId);
        }
        catch (JsonException ex)
        {
            throw Invalid(
                response,
                $"The TypeSafe API returned a response that could not be read: {ex.Message}",
                fieldPath: null,
                ex);
        }
    }

    private static void RecordUsage(SystemOneResult result)
    {
        var usage = result.Usage;
        if (usage is null)
        {
            return;
        }

        var model = new KeyValuePair<string, object?>("typesafe.model", result.Model);

        if (usage.InputTokens is { } input)
        {
            TypeSafeTelemetry.InputTokens.Record(input, model);
        }

        if (usage.OutputTokens is { } output)
        {
            TypeSafeTelemetry.OutputTokens.Record(output, model);
        }
    }

    private static TypeSafeResponseValidationException Invalid(
        TypeSafeHttpResponse response,
        string message,
        string? fieldPath,
        Exception? innerException = null)
    {
        var exception = innerException is null
            ? new TypeSafeResponseValidationException(message)
            : new TypeSafeResponseValidationException(message, innerException);

        exception.StatusCode = response.StatusCode;
        exception.RequestId = response.RequestId;
        exception.Headers = response.Headers;
        exception.FieldPath = fieldPath;
        exception.DocumentationUrl = DocumentationUrl;

        return exception;
    }
}
