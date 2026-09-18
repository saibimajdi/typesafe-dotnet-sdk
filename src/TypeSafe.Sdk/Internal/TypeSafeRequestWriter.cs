using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;
using TypeSafeAI.Serialization;

namespace TypeSafeAI.Internal;

/// <summary>
/// Serializes a System One request body.
/// </summary>
/// <remarks>
/// The body is written with a single <see cref="Utf8JsonWriter"/> rather than assembled as a
/// <see cref="JsonObject"/>, which avoids re-parenting a caller's <see cref="JsonNode"/> instances
/// and produces bytes that can be reused across retry attempts without re-serializing.
/// </remarks>
internal static class TypeSafeRequestWriter
{
    /// <summary>
    /// The field name removed in API v1. Sending it fails validation, so it is rejected locally
    /// with a message that explains the migration rather than costing a round trip.
    /// </summary>
    private const string RemovedField = "document";

    /// <summary>
    /// Serializes a System One request body.
    /// </summary>
    /// <param name="state">The state to evaluate.</param>
    /// <param name="model">The resolved model name.</param>
    /// <param name="questions">The questions to ask.</param>
    /// <param name="additional">Extra top-level fields, merged last.</param>
    /// <returns>The UTF-8 encoded request body.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="questions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="state"/> is <see langword="null"/>, no questions were supplied, two questions
    /// share an id, or <paramref name="additional"/> contains the removed <c>document</c> field.
    /// </exception>
    public static byte[] Write(
        JsonNode? state,
        string model,
        IEnumerable<Question> questions,
        IReadOnlyDictionary<string, JsonNode?>? additional)
    {
        ArgumentNullException.ThrowIfNull(questions);

        if (state is null)
        {
            // The JavaScript types allow a null state and the Python SDK rejects it client-side.
            // Rejecting is the conservative reading of the documentation: every documented
            // example carries a state, and values inside an object may be null instead.
            throw new ArgumentException(
                "The request state cannot be null. Pass text, a JSON object, or a JSON array. " +
                "Null values are allowed inside an object or array, just not as the state itself.",
                nameof(state));
        }

        ValidateAdditional(additional);

        var (orderedIds, byId) = IndexQuestions(questions);

        var buffer = new ArrayBufferWriter<byte>(initialCapacity: 1024);

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            // The SDK's own fields are written first, and any colliding caller-supplied field is
            // written in its place, so the documented last-write-wins merge falls out of the
            // ordering rather than producing duplicate JSON keys.
            WriteState(writer, state, additional);
            WriteModel(writer, model, additional);
            WriteQuestions(writer, orderedIds, byId, additional);
            WriteRemainingAdditionalFields(writer, additional);

            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void ValidateAdditional(IReadOnlyDictionary<string, JsonNode?>? additional)
    {
        if (additional is null)
        {
            return;
        }

        if (additional.ContainsKey(RemovedField))
        {
            throw new ArgumentException(
                $"The '{RemovedField}' field was removed in TypeSafe API v1 and a request carrying " +
                "it fails validation. Send the content as 'state' instead. See " +
                "https://docs.typesafe.ai/migrating-to-v1.",
                nameof(additional));
        }
    }

    private static (List<string> OrderedIds, Dictionary<string, Question> ById) IndexQuestions(
        IEnumerable<Question> questions)
    {
        var orderedIds = new List<string>();
        var byId = new Dictionary<string, Question>(StringComparer.Ordinal);

        foreach (var question in questions)
        {
            ArgumentNullException.ThrowIfNull(question, nameof(questions));

            if (!byId.TryAdd(question.Id, question))
            {
                throw new ArgumentException(
                    $"The question id '{question.Id}' appears more than once. Ids are the keys of " +
                    "the request's questions map and of the response's answers map, so they must be " +
                    "unique within a request.",
                    nameof(questions));
            }

            orderedIds.Add(question.Id);
        }

        if (orderedIds.Count == 0)
        {
            throw new ArgumentException(
                "A request must contain at least one question. Sending many questions in one " +
                "request is far cheaper and faster than one request per question, so adding " +
                "speculative questions is close to free.",
                nameof(questions));
        }

        return (orderedIds, byId);
    }

    private static void WriteNode(Utf8JsonWriter writer, string name, JsonNode? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WritePropertyName(name);
            value.WriteTo(writer);
        }
    }

    private static void WriteState(
        Utf8JsonWriter writer,
        JsonNode? state,
        IReadOnlyDictionary<string, JsonNode?>? additional)
    {
        if (additional is not null && additional.ContainsKey("state"))
        {
            WriteNode(writer, "state", additional["state"]);
        }
        else
        {
            writer.WritePropertyName("state");
            state!.WriteTo(writer);
        }
    }

    private static void WriteModel(
        Utf8JsonWriter writer,
        string model,
        IReadOnlyDictionary<string, JsonNode?>? additional)
    {
        if (additional is not null && additional.ContainsKey("model"))
        {
            WriteNode(writer, "model", additional["model"]);
        }
        else
        {
            writer.WriteString("model", model);
        }
    }

    private static void WriteQuestions(
        Utf8JsonWriter writer,
        List<string> orderedIds,
        Dictionary<string, Question> byId,
        IReadOnlyDictionary<string, JsonNode?>? additional)
    {
        if (additional is not null && additional.ContainsKey("questions"))
        {
            WriteNode(writer, "questions", additional["questions"]);
            return;
        }

        writer.WriteStartObject("questions");

        foreach (var id in orderedIds)
        {
            writer.WritePropertyName(id);
            QuestionWriter.Write(writer, byId[id]);
        }

        writer.WriteEndObject();
    }

    private static void WriteRemainingAdditionalFields(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, JsonNode?>? additional)
    {
        if (additional is null)
        {
            return;
        }

        foreach (var (name, value) in additional)
        {
            if (name is "state" or "model" or "questions")
            {
                continue;
            }

            WriteNode(writer, name, value);
        }
    }
}
