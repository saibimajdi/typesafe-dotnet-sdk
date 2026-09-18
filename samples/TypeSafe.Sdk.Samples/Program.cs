using System.Text.Json.Nodes;

namespace TypeSafeAI.Samples;

/// <summary>
/// Runnable examples of the patterns the TypeSafe documentation describes.
/// </summary>
/// <remarks>
/// <para>
/// Run with an API key in the environment:
/// </para>
/// <code>
/// TYPESAFE_API_KEY=tsk_... dotnet run --project samples/TypeSafe.Sdk.Samples
/// </code>
/// <para>
/// Without a key the program prints how to set one and exits, so the project still builds and runs
/// in a fresh clone.
/// </para>
/// </remarks>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TypeSafeDefaults.ApiKeyEnvironmentVariable)))
        {
            Console.Error.WriteLine(
                $"Set {TypeSafeDefaults.ApiKeyEnvironmentVariable} to run the samples. " +
                "Create a key at https://console.typesafe.ai/.");
            return 1;
        }

        using var client = new TypeSafeClient();

        var scenario = args.Length > 0 ? args[0] : "triage";

        var run = scenario switch
        {
            "triage" => (Func<ITypeSafeClient, Task>)TriageAsync,
            "routing" => ConfidenceGatedRoutingAsync,
            "composite" => CompositeScoringAsync,
            "batch" => SpeculativeFanOutAsync,
            _ => null,
        };

        if (run is null)
        {
            // A sample should not greet a typo with a stack trace.
            Console.Error.WriteLine($"Unknown scenario '{scenario}'. Try: triage, routing, composite, batch.");
            return 1;
        }

        await run(client);
        return 0;
    }

    /// <summary>
    /// One request, three question kinds, one typed answer each.
    /// </summary>
    /// <remarks>
    /// This is the shape almost every TypeSafe integration starts from: put everything the decision
    /// needs into the state, ask every question that shares that state in a single call, then act on
    /// the answers with ordinary code.
    /// </remarks>
    private static async Task TriageAsync(ITypeSafeClient client)
    {
        Console.WriteLine("== Triage: one request, three questions, three answers ==\n");

        var state = new JsonObject
        {
            ["ticket"] = new JsonObject
            {
                ["subject"] = "Duplicate charge",
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["from"] = "customer",
                        ["text"] = "I was charged twice for order A-104. Please refund the duplicate.",
                    },
                    new JsonObject
                    {
                        ["from"] = "support",
                        ["text"] = "We are checking the charges.",
                    },
                },
            },
            ["order"] = new JsonObject
            {
                ["id"] = "A-104",
                ["charges"] = new JsonArray
                {
                    new JsonObject { ["amount_usd"] = 49, ["status"] = "captured" },
                    new JsonObject { ["amount_usd"] = 49, ["status"] = "captured" },
                },
            },
            ["refund_policy"] = "Duplicate charges are eligible for a refund.",
        };

        // Backticked dot-and-index paths tell the model exactly which part of the state to judge.
        var refundRequested = new NoulQuestion(
            "refund_requested",
            "Does `ticket.messages[0].text` request a refund?");

        var policySupports = new NoulQuestion(
            "policy_supports_refund",
            "Does `refund_policy` support the refund requested in `ticket.messages[0].text`, " +
            "given `order.charges`?");

        var department = new ChoiceQuestion(
            "department",
            "Which team should handle this?",
            new Dictionary<string, string?>
            {
                ["billing"] = "Payments, invoicing, refunds",
                ["technical"] = "Bugs, outages, integrations",
                ["sales"] = "Pricing, upgrades, new accounts",
            });

        var frustration = new ScoreQuestion(
            "frustration",
            "How frustrated does the customer appear in `ticket.messages[0].text`?",
            ["Calm and neutral.", "Concerned but civil.", "Very angry or using strong language."]);

        var result = await client.SystemOneAsync(state, [refundRequested, policySupports, department, frustration]);

        // Typed retrieval: no string keys and no casts, so renaming a question is a compile error.
        Console.WriteLine($"model            : {result.Model}");
        Console.WriteLine($"refund requested : {result.Get(refundRequested).Probability:P0}");
        Console.WriteLine($"policy supports  : {result.Get(policySupports).Probability:P0}");
        Console.WriteLine($"department       : {result.Get(department).Label} " +
                          $"(confidence {result.Get(department).Confidence:P0})");
        Console.WriteLine($"frustration      : {result.Get(frustration).Score:0.00} of 2");

        if (result.Usage is { } usage)
        {
            Console.WriteLine($"tokens           : {usage.InputTokens} in / {usage.OutputTokens} out");
        }

        // The decision is ordinary code, and the thresholds live here rather than in a prompt.
        var shouldRefund = result.Get(refundRequested).Probability > 0.8 &&
                           result.Get(policySupports).Probability > 0.8;

        Console.WriteLine($"\n-> {(shouldRefund ? "Refund the duplicate charge." : "Route to a human.")}");
    }

    /// <summary>
    /// Uses confidence as a second axis: the answer says what, confidence says whether to act.
    /// </summary>
    /// <remarks>
    /// The API reports confidence on choice and score answers. The SDK deliberately ships no default
    /// thresholds, because where to draw them depends on the cost of getting it wrong.
    /// </remarks>
    private static async Task ConfidenceGatedRoutingAsync(ITypeSafeClient client)
    {
        Console.WriteLine("== Confidence-gated routing ==\n");

        const string Message = "Can you approve the withdrawal I set up yesterday?";

        var action = new ChoiceQuestion(
            "action",
            "What is the user trying to do?",
            new Dictionary<string, string?>
            {
                ["check_balance"] = "View the account balance",
                ["approve_transfer"] = "Approve a pending withdrawal",
                ["support"] = "Get help with an issue",
            });

        var result = await client.SystemOneAsync(Message, [action]);
        var answer = result.Get(action);

        // One low-confidence floor catches anything the model reports as genuinely uncertain.
        const double Uncertain = 0.5;

        // Above that floor the bar is higher for a destructive action than for a read-only one.
        const double ActOnIrreversible = 0.9;

        Console.WriteLine($"action     : {answer.Label}");
        Console.WriteLine($"confidence : {answer.Confidence:P0}");
        Console.WriteLine($"ranked     : {string.Join(", ", answer.Ranked().Select(pair => $"{pair.Key} {pair.Value:P0}"))}");

        var decision = answer.Confidence < Uncertain
            ? "route to a human: the model is genuinely unsure"
            : answer.Label switch
            {
                "check_balance" => "show the balance: showing the wrong screen is recoverable",
                "approve_transfer" when answer.Confidence > ActOnIrreversible => "confirm, then execute",
                "approve_transfer" => "ask the user to confirm first: high stakes, moderate confidence",
                _ => "open a support conversation",
            };

        Console.WriteLine($"\n-> {decision}");
    }

    /// <summary>
    /// Breaks one complex judgment into several Scores and combines them with weights held in code.
    /// </summary>
    /// <remarks>
    /// This is the composite scoring pattern. When the combined result does not match what the team
    /// would decide, the weights change in code and no prompt is re-written.
    /// </remarks>
    private static async Task CompositeScoringAsync(ITypeSafeClient client)
    {
        Console.WriteLine("== Composite scoring ==\n");

        const string Report = """
            Our API integration started returning 500 errors on every request about 20 minutes ago,
            and we cannot process any customer orders until this is fixed. Logs show a null reference
            in the payment callback handler. This is the third time this month.
            """;

        // One question per factor, so each is a snap judgment rather than one overloaded prompt.
        var severity = new ScoreQuestion(
            "severity",
            "How severe is the impact described?",
            ["Cosmetic", "Degraded", "Major feature broken", "Everything is down"]);

        var frustration = new ScoreQuestion(
            "frustration",
            "How frustrated does the reporter appear?",
            ["Calm", "Concerned", "Frustrated", "Very angry"]);

        var actionability = new ScoreQuestion(
            "actionability",
            "How much does the report give an engineer to work with?",
            ["Nothing", "Vague symptom", "Clear symptom", "Exact cause identified"]);

        var result = await client.SystemOneAsync(Report, [severity, frustration, actionability]);

        var severityAnswer = result.Get(severity);
        var frustrationAnswer = result.Get(frustration);
        var actionabilityAnswer = result.Get(actionability);

        Console.WriteLine($"severity     : {severityAnswer.Score:0.00} / 3  (normalised {severityAnswer.NormalizedScore:P0})");
        Console.WriteLine($"frustration  : {frustrationAnswer.Score:0.00} / 3  (normalised {frustrationAnswer.NormalizedScore:P0})");
        Console.WriteLine($"actionability: {actionabilityAnswer.Score:0.00} / 3  (normalised {actionabilityAnswer.NormalizedScore:P0})");

        // Each answer is normalised by its own rubric length, so rubrics of different lengths
        // combine meaningfully. The weights are the only place priority policy lives.
        var priority = CompositeScore.Weighted(
            new WeightedScore(severityAnswer, 0.5),
            new WeightedScore(frustrationAnswer, 0.2),
            new WeightedScore(actionabilityAnswer, 0.3));

        Console.WriteLine($"\nweighted priority: {priority:P0}");

        // The same answers can be re-scored against different roles without paying for inference
        // again, which is why answers are serializable.
        var byProfile = CompositeScore.Profiles(
            [new WeightedScore(severityAnswer, 0), new WeightedScore(frustrationAnswer, 0), new WeightedScore(actionabilityAnswer, 0)],
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["on-call engineer"] = new Dictionary<string, double>
                {
                    ["severity"] = 0.6,
                    ["actionability"] = 0.4,
                },
                ["support lead"] = new Dictionary<string, double>
                {
                    ["severity"] = 0.3,
                    ["frustration"] = 0.7,
                },
            });

        foreach (var (profile, score) in byProfile)
        {
            Console.WriteLine($"  as {profile,-16}: {score:P0}");
        }

        Console.WriteLine($"\n-> {(priority > 0.6 ? "Page the on-call engineer." : "Queue for the next business day.")}");
    }

    /// <summary>
    /// Asks every question the code might need in one request, including speculative ones.
    /// </summary>
    /// <remarks>
    /// Questions in a request are evaluated in parallel, so adding one barely changes response time
    /// and costs only its own tokens. Asking a question whose answer may go unused is close to free,
    /// and it removes the second round trip that a follow-up question would otherwise need.
    /// </remarks>
    private static async Task SpeculativeFanOutAsync(ITypeSafeClient client)
    {
        Console.WriteLine("== Speculative fan-out ==\n");

        const string Message = "The export button does nothing when I click it. Also, can I get an invoice for last month?";

        var questions = new QuestionSet
        {
            new ChoiceQuestion(
                "department",
                "Which single team should own this message?",
                new Dictionary<string, string?>
                {
                    ["billing"] = "Payments, invoicing, refunds",
                    ["technical"] = "Bugs, outages, integrations",
                    ["sales"] = "Pricing, upgrades, new accounts",
                }),

            new NoulQuestion("is_urgent", "Does the message convey urgency or time-sensitivity?"),
            new NoulQuestion("mentions_bug", "Does the message report something not working as expected?"),
            new NoulQuestion("mentions_invoice", "Does the message ask for an invoice or billing document?"),

            // Speculative: only meaningful if the message turns out to be a bug report.
            new ScoreQuestion(
                "bug_severity",
                "If `mentions_bug` is true, how much does the reported problem block the reporter?",
                ["Not at all", "Slows them down", "Blocks one task", "Blocks all work"]),

            new ChoiceQuestion(
                "sentiment",
                "What is the overall tone of the message?",
                new Dictionary<string, string?>
                {
                    ["neutral"] = null,
                    ["frustrated"] = null,
                    ["angry"] = null,
                }),
        };

        var result = await client.SystemOneAsync(Message, questions);

        Console.WriteLine($"questions asked : {questions.Count}");
        Console.WriteLine($"answers returned: {result.Answers.Count}");
        Console.WriteLine();

        foreach (var id in result.Ids)
        {
            var description = result.Get(id) switch
            {
                NoulAnswer noul => $"probability {noul.Probability:P0}",
                ChoiceAnswer choice => $"label {choice.Label} (confidence {choice.Confidence:P0})",
                ScoreAnswer score => $"score {score.Score:0.00} (confidence {score.Confidence:P0})",
                UnknownAnswer unknown => $"unrecognised kind '{unknown.Type}'",
                _ => "unknown",
            };

            Console.WriteLine($"  {id,-18} {description}");
        }

        // The speculative answer is only read when the branch that needs it is taken, which is what
        // makes asking it up front worthwhile rather than wasteful.
        var isBug = result.TryGet("mentions_bug", out Answer? maybeNoul) &&
                    maybeNoul is NoulAnswer { Probability: > 0.5 };

        if (isBug)
        {
            var severity = result.TryGet("bug_severity", out Answer? maybeScore) && maybeScore is ScoreAnswer reported
                ? reported.Score
                : 0;

            Console.WriteLine($"\n-> Bug report detected; severity score {severity:0.00}. Open a technical ticket.");
        }
        else
        {
            Console.WriteLine("\n-> No bug report detected; route on the department answer alone.");
        }
    }
}
