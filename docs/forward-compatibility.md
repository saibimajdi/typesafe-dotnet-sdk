# Forward compatibility

The API evolves. This SDK is built so that an API feature newer than the SDK is never blocked by it,
and so that adopting a new feature never breaks an existing one.

There are three levels to that, from the cheapest to the most explicit:

| Level | Mechanism | Use it when |
| --- | --- | --- |
| Known question, extra field | `additionalProperties` on a question, `AdditionalProperties` on a request, `AdditionalBodyProperties` on `TypeSafeRequestOptions` | The API grew a field on a question or request body the SDK already models. |
| Unknown question kind | `RawQuestion` | The API grew a question kind the SDK does not model. |
| Unknown answer kind, or the whole body | `UnknownAnswer`, `RawJson`, `Answer.AdditionalProperties` | The API grew an answer kind or a response field the SDK does not model. |

## Extra request fields

Every question type accepts an `additionalProperties` map, merged into the question's JSON object
last, so those fields win over the SDK's own:

```csharp
using System.Text.Json.Nodes;
using TypeSafeAI;

var question = new NoulQuestion(
    "is_urgent",
    "Does this convey urgency?",
    criteria: null,
    additionalProperties: new Dictionary<string, JsonNode?>
    {
        ["weight"] = 0.5,
    });
```

A whole request accepts the same thing at the top level of the body, through
`SystemOneRequest.AdditionalProperties`:

```csharp
using System.Text.Json.Nodes;
using TypeSafeAI;

var result = await client.SystemOneAsync(new SystemOneRequest
{
    State = "My card was charged twice, please fix this ASAP.",
    Questions = [new NoulQuestion("is_urgent", "Does this convey urgency?")],
    AdditionalProperties = new Dictionary<string, JsonNode?>
    {
        ["temperature"] = 0.2,
        ["new_api_option"] = new JsonObject { ["mode"] = "strict" },
    },
});
```

For one call only, `TypeSafeRequestOptions.AdditionalBodyProperties` does the same without changing
the request object:

```csharp
using System.Text.Json.Nodes;
using TypeSafeAI;

var result = await client.SystemOneAsync(new SystemOneRequest
{
    State = "My card was charged twice, please fix this ASAP.",
    Questions = [new NoulQuestion("is_urgent", "Does this convey urgency?")],
    Options = new TypeSafeRequestOptions
    {
        AdditionalBodyProperties = new Dictionary<string, JsonNode?>
        {
            ["temperature"] = 0.2,
        },
    },
});
```

Merging is shallow. An object value replaces an existing value rather than merging into it, so if
the API adds a nested field inside an object the SDK already sends, send the whole object.

## An unknown question kind

`RawQuestion` sends exactly what you give it. Everything except `type` is passed through verbatim:

```csharp
using System.Text.Json.Nodes;
using TypeSafeAI;

var question = new RawQuestion(
    "sentiment_strength",
    "spectrum",
    new JsonObject
    {
        ["instructions"] = "How strong is the sentiment in this message?",
        ["criteria"] = new JsonArray("weak", "neutral", "strong"),
    });

var result = await client.SystemOneAsync("This is the third time I have had to ask.", [question]);
```

Two details worth knowing. The `id` is supplied separately and is never part of the body. And `Body`
is a **live** `JsonObject`: mutating it changes what is sent on the next call, which is deliberate —
it is the one place where you can adjust a question between attempts — but it also makes
`RawQuestion` the only question type that is not immutable.

A raw question's answer deserializes to `UnknownAnswer`, unless its kind happens to match one the
SDK models, in which case you get the normal typed answer.

## An unknown answer kind

An unrecognised answer does not fail the response. The SDK deserializes it into `UnknownAnswer`,
keeps the complete body in `Raw`, logs a warning, and leaves every other answer in the same payload
untouched.

```csharp
using TypeSafeAI;

foreach (var (id, unknown) in result.UnknownAnswers)
{
    Console.WriteLine($"'{id}' was answered with the unmodelled kind '{unknown.Type}'.");

    if (unknown.Raw.TryGetProperty("spectrum", out var spectrum) && spectrum.TryGetDouble(out var value))
    {
        Console.WriteLine($"  spectrum = {value}");
    }
    else
    {
        // Nothing modelled and nothing guessed: the raw body is all there is, and it is enough.
        Console.WriteLine($"  raw = {unknown.Raw.GetRawText()}");
    }
}
```

`UnknownAnswers` is usually empty. A non-empty result means the API introduced an answer kind after
this SDK release. Callers that understand the new kind can read `Raw` directly; callers that do not
can ignore it and keep working. That is the whole point: adopting a new API feature never breaks an
existing one.

## Extra response fields

`Answer.AdditionalProperties` holds the fields of a single answer that this SDK version does not
model:

```csharp
using TypeSafeAI;

var urgency = result.Noul("is_urgent");

if (urgency.AdditionalProperties.TryGetValue("new_answer_field", out var value))
{
    Console.WriteLine(value?.ToJsonString());
}
```

`SystemOneResult.RawJson` holds the entire response body, exactly as it arrived, which is the escape
hatch for anything at the top level:

```csharp
using TypeSafeAI;

if (result.RawJson.TryGetProperty("new_top_level_field", out var extra))
{
    Console.WriteLine(extra.GetRawText());
}
```

`ModelsResult.RawJson` does the same for the models endpoint.

## Round-tripping

Unmodelled fields survive serialization, so a cached result is still complete when it is read back:

```csharp
using TypeSafeAI;
using TypeSafeAI.Serialization;

var json = TypeSafeJson.Serialize(result);
var replayed = TypeSafeJson.DeserializeResult(json);

// Including the answers whose kind this SDK version does not model.
foreach (var (id, unknown) in replayed.UnknownAnswers)
{
    Console.WriteLine($"{id}: {unknown.Type}");
}
```

`TypeSafeJson` uses the SDK's own converters instead of reflection, so the round-trip is safe under
trimming and ahead-of-time compilation. `SystemOneResult.RequestId` is not part of the response body
and is therefore not included; persist it separately if you need it later.

## When to ask for first-class support

The escape hatches exist so nothing is ever blocked, not so that everything stays raw. If you are
using `RawQuestion` or reading `Answer.AdditionalProperties` for something you rely on regularly,
that is a good [feature request](https://github.com/saibimajdi/typesafeai-dotnet-sdk/blob/main/.github/ISSUE_TEMPLATE/feature_request.yml): a modelled type
gets XML documentation, compile-time type checking, and a place in the tracked public API surface,
none of which a raw body can offer.
