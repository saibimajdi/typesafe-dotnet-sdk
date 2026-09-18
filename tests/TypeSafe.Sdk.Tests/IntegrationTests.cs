using System.Text.Json.Nodes;

using System.Text.Json;

namespace TypeSafeAI.Tests;

/// <summary>
/// Tests that call the real TypeSafe API.
/// </summary>
/// <remarks>
/// <para>
/// These are skipped unless <c>TYPESAFE_API_KEY</c> is set, so the default <c>dotnet test</c> run
/// is hermetic and free. To run them:
/// </para>
/// <code>
/// TYPESAFE_API_KEY=tsk_... dotnet test --solution TypeSafe.slnx
/// </code>
/// <para>
/// They exist because three facts could not be settled from the documentation alone: whether
/// <c>legend</c> can carry structured values, whether error responses carry
/// <c>x-typesafe-request-id</c>, and whether the API emits <c>Retry-After</c>. The probes here
/// answer them against the live service.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public sealed class IntegrationTests
{
    private static string? ApiKey => Environment.GetEnvironmentVariable(TypeSafeDefaults.ApiKeyEnvironmentVariable);

    private static TypeSafeClient? CreateClient()
    {
        var key = ApiKey;
        return string.IsNullOrWhiteSpace(key) ? null : new TypeSafeClient(key);
    }

    [Fact]
    public async Task TheDocumentedThreeQuestionRequestRoundTrips()
    {
        using var client = CreateClient();
        Assert.SkipWhen(client is null, $"Set {TypeSafeDefaults.ApiKeyEnvironmentVariable} to run integration tests.");

        var result = await client!.SystemOneAsync(
            new JsonObject
            {
                ["ticket_message"] = "My flight was cancelled. Can I get a refund?",
                ["refund_policy"] = "Cancelled flights are eligible for a full refund.",
            },
            [
                new NoulQuestion("refund_requested", "Does `ticket_message` request a refund?"),
                new ChoiceQuestion(
                    "request_type",
                    "What is the main request in `ticket_message`?",
                    new Dictionary<string, string?>
                    {
                        ["refund"] = "The customer wants money returned.",
                        ["rebooking"] = "The customer wants a replacement flight.",
                        ["information"] = "The customer is asking for information only.",
                    }),
                new ScoreQuestion(
                    "frustration",
                    "How frustrated does the customer appear in `ticket_message`?",
                    ["Calm and neutral.", "Concerned but civil.", "Very angry or using strong language."]),
            ],
            TestContext.Current.CancellationToken);

        // The documented contract: a probability in [0, 1], a label drawn from the criteria, and a
        // score inside the rubric.
        Assert.InRange(result.Noul("refund_requested").Probability, 0, 1);
        Assert.Contains(result.Choice("request_type").Label, new[] { "refund", "rebooking", "information" });
        Assert.InRange(result.Score("frustration").Score, 0, 2);

        // The response reports the resolved model, not the jev-latest alias that was requested.
        Assert.False(string.IsNullOrWhiteSpace(result.Model));

        // A request id is documented for errors; confirm it is present on success too.
        Assert.False(string.IsNullOrWhiteSpace(result.RequestId));

        // The probabilities must actually be a distribution.
        var total = result.Choice("request_type").Probabilities.Values.Sum();
        Assert.InRange(total, 0.99, 1.01);
    }

    [Fact]
    public async Task StructuredScoreLevelsComeBackAsStructuredLegendValues()
    {
        using var client = CreateClient();
        Assert.SkipWhen(client is null, $"Set {TypeSafeDefaults.ApiKeyEnvironmentVariable} to run integration tests.");

        // The HTTP reference types legend values as strings, while both sibling SDKs type them as
        // free-form JSON. This settles it against the live service.
        var result = await client!.SystemOneAsync(
            "Invoice #4471 issued March 3, 2026 to Beaver Dam Logistics for $12,840.00, net 30.",
            [
                new ScoreQuestion(
                    "amount_due",
                    "How large is the invoice total?",
                    new JsonNode?[]
                    {
                        new JsonObject { ["label"] = "small", ["max_usd"] = 1000 },
                        new JsonObject { ["label"] = "medium", ["max_usd"] = 10000 },
                        new JsonObject { ["label"] = "large", ["max_usd"] = 100000 },
                    }),
            ],
            TestContext.Current.CancellationToken);

        var answer = result.Score("amount_due");
        var firstLevel = answer.LegendAtLevel(0);

        Assert.NotNull(firstLevel);
        Assert.Equal(JsonValueKind.Object, firstLevel!.GetValueKind());
        Assert.Equal("small", firstLevel["label"]!.GetValue<string>());
    }

    [Fact]
    public async Task AnInvalidRequestFailsWith422AndARequestId()
    {
        using var client = CreateClient();
        Assert.SkipWhen(client is null, $"Set {TypeSafeDefaults.ApiKeyEnvironmentVariable} to run integration tests.");

        // Score rubrics are validated locally, so a raw question is the only way to reach the
        // server's own validation path. An empty rubric is rejected server-side.
        var exception = await Assert.ThrowsAnyAsync<TypeSafeApiException>(() => client!.SystemOneAsync(
            "text",
            [new RawQuestion("bad", "score", new JsonObject { ["criteria"] = new JsonArray() })],
            TestContext.Current.CancellationToken));

        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, exception.StatusCode);

        // Documented for errors, and load-bearing for support: confirm the header really is present.
        Assert.False(string.IsNullOrWhiteSpace(exception.RequestId));
    }

    [Fact]
    public async Task ModelsCanBeListed()
    {
        using var client = CreateClient();
        Assert.SkipWhen(client is null, $"Set {TypeSafeDefaults.ApiKeyEnvironmentVariable} to run integration tests.");

        var result = await client!.Models.ListAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(result.Models);
        Assert.Contains(result.Models, static model => model.Name.Length > 0);
    }

    [Fact]
    public async Task AnInvalidApiKeyIsRejectedWith401()
    {
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(ApiKey),
            $"Set {TypeSafeDefaults.ApiKeyEnvironmentVariable} to run integration tests.");

        using var client = new TypeSafeClient("tsk_definitely_not_a_valid_key", new TypeSafeClientOptions
        {
            Retry = RetryPolicy.None,
        });

        var exception = await Assert.ThrowsAsync<TypeSafeAuthenticationException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        Assert.Equal("authentication_error", exception.ErrorType);
        Assert.False(string.IsNullOrWhiteSpace(exception.RequestId));
    }
}
