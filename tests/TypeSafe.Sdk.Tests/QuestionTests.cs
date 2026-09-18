using System.Text.Json.Nodes;

using System.Text.Json;

namespace TypeSafeAI.Tests;

/// <summary>
/// Verifies that the documented request rules are enforced before a network call is made.
/// </summary>
public sealed class QuestionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AQuestionIdMustNotBeBlank(string id)
    {
        Assert.Throws<ArgumentException>(() => new NoulQuestion(id, "question?"));
    }

    [Fact]
    public void AQuestionWithoutAnIdIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new NoulQuestion(null!, "question?"));
    }

    [Fact]
    public void AScoreRubricNeedsAtLeastTwoLevels()
    {
        // The API rejects a single-level rubric, so catching it locally saves a round trip.
        var exception = Assert.Throws<ArgumentException>(
            () => new ScoreQuestion("s", "q?", ["only one"]));

        Assert.Contains("at least 2", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AScoreRubricAcceptsAtMostTenLevels()
    {
        // Eleven levels is a documented server error.
        var eleven = Enumerable.Range(0, 11).Select(i => $"level {i}").ToList();

        var exception = Assert.Throws<ArgumentException>(() => new ScoreQuestion("s", "q?", eleven));
        Assert.Contains("at most 10", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AScoreRubricOfTwoAndOfTenAreBothAccepted()
    {
        Assert.Equal(2, new ScoreQuestion("s", "q?", ["low", "high"]).LevelCount);
        Assert.Equal(10, new ScoreQuestion("s", "q?", Enumerable.Range(0, 10).Select(i => $"l{i}")).LevelCount);
    }

    [Fact]
    public void AScoreRubricPreservesItsOrder()
    {
        var question = new ScoreQuestion("s", "q?", ["low", "medium", "high"]);

        // The order of the array is the numbering, so it must never be reordered.
        Assert.Equal(
            ["low", "medium", "high"],
            question.Criteria.Select(static node => node!.GetValue<string>()));
    }

    [Fact]
    public void AScoreRubricAllowsNullLevels()
    {
        // The structure documentation types score levels as the same free-form entry type as
        // instructions, which includes null.
        var question = new ScoreQuestion("s", "q?", [JsonValue.Create("low"), null]);

        Assert.Null(question.Criteria[1]);
    }

    [Fact]
    public void AChoiceNeedsAtLeastOneOption()
    {
        Assert.Throws<ArgumentException>(() => new ChoiceQuestion("c", "q?", Array.Empty<string>()));
    }

    [Fact]
    public void AChoiceRejectsDuplicateOptionLabels()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ChoiceQuestion("c", "q?", ["calm", "angry", "calm"]));

        Assert.Contains("more than once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AChoiceAcceptsUpToTwoHundredAndFiftyFiveOptions()
    {
        var maximal = Enumerable.Range(0, 255).Select(i => $"option_{i}").ToList();
        Assert.Equal(255, new ChoiceQuestion("c", "q?", maximal).Labels.Count);

        var tooMany = Enumerable.Range(0, 256).Select(i => $"option_{i}").ToList();
        Assert.Throws<ArgumentException>(() => new ChoiceQuestion("c", "q?", tooMany));
    }

    [Fact]
    public void AChoicePreservesOptionOrder()
    {
        var question = new ChoiceQuestion("c", "q?", ["billing", "technical", "sales"]);

        // Order is preserved for deterministic serialization, even though the API treats the
        // options as an unordered set.
        Assert.Equal(["billing", "technical", "sales"], question.Labels);
    }

    [Fact]
    public void AChoiceAcceptsStructuredOptionDescriptions()
    {
        var question = new ChoiceQuestion(
            "c",
            "q?",
            new Dictionary<string, JsonNode?>
            {
                ["merge"] = new JsonObject { ["action"] = "merge" },
                ["skip"] = null,
            });

        Assert.Equal(JsonValueKind.Object, question.Criteria["merge"]!.GetValueKind());
        Assert.Null(question.Criteria["skip"]);
    }

    [Fact]
    public void ANoulCriteriaMustDescribeAtLeastOneOutcome()
    {
        var exception = Assert.Throws<ArgumentException>(() => new NoulCriteria());

        Assert.Contains("at least one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANoulCriteriaMayDescribeOnlyOneOutcome()
    {
        var criteria = new NoulCriteria(@true: JsonValue.Create("time-sensitive"));

        Assert.NotNull(criteria.True);
        Assert.Null(criteria.False);
    }

    [Fact]
    public void InstructionsAcceptStructuredJson()
    {
        // The SDK must not flatten structured instructions to a string.
        var question = new NoulQuestion(
            "invoice_ok",
            new JsonObject { ["field"] = new JsonObject { ["name"] = "invoice_number" } });

        Assert.Equal(JsonValueKind.Object, question.Instructions!.GetValueKind());
    }

    [Fact]
    public void AStringInstructionIsConvertedToAJsonString()
    {
        var question = new NoulQuestion("a", "Is this urgent?");

        Assert.Equal("Is this urgent?", question.Instructions!.GetValue<string>());
    }

    [Fact]
    public void InstructionsAreOptional()
    {
        var question = new NoulQuestion("a");

        Assert.Null(question.Instructions);
    }

    [Fact]
    public void EachQuestionKnowsItsWireType()
    {
        Assert.Equal("noul", new NoulQuestion("a", "q?").Type);
        Assert.Equal("choice", new ChoiceQuestion("b", "q?", ["x"]).Type);
        Assert.Equal("score", new ScoreQuestion("c", "q?", ["x", "y"]).Type);
        Assert.Equal("future", new RawQuestion("d", "future", new JsonObject()).Type);
    }

    [Fact]
    public void AQuestionSetRejectsDuplicateIds()
    {
        var set = new QuestionSet { new NoulQuestion("a", "q?") };

        var exception = Assert.Throws<ArgumentException>(() => set.Add(new NoulQuestion("a", "other?")));
        Assert.Contains("more than once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AQuestionSetSupportsCollectionInitializersAndLookup()
    {
        var set = new QuestionSet
        {
            new NoulQuestion("a", "q?"),
            new NoulQuestion("b", "q?"),
        };

        Assert.Equal(2, set.Count);
        Assert.True(set.Contains("a"));
        Assert.True(set.TryGet("b", out var found));
        Assert.Equal("b", found!.Id);
        Assert.Equal("a", set["a"].Id);
    }

    [Fact]
    public async Task QuestionsBindAnswersAtCompileTime()
    {
        var (client, _) = TestClient.Returning(TestClient.TriageResponse);

        var isUrgent = new NoulQuestion("is_urgent", "Does this convey urgency?");
        var department = new ChoiceQuestion("department", "Which team?", ["billing", "technical", "sales"]);
        var frustration = new ScoreQuestion("frustration", "How frustrated?", ["Calm", "Frustrated", "Very angry"]);

        var result = await client.SystemOneAsync("state", [isUrgent, department, frustration]);

        // These need neither a string key nor a cast, so renaming a question becomes a compile
        // error rather than a runtime miss.
        NoulAnswer urgent = result.Get(isUrgent);
        ChoiceAnswer choice = result.Get(department);
        ScoreAnswer score = result.Get(frustration);

        Assert.Equal(0.92, urgent.Probability);
        Assert.Equal("technical", choice.Label);
        Assert.Equal(1.6, score.Score);

        Assert.True(result.TryGet(isUrgent, out var tried));
        Assert.Equal(0.92, tried!.Probability);
    }

    [Fact]
    public void CompositeScoresCombineNormalizedRubrics()
    {
        // Both rubrics are three levels, so a score of 1.0 normalises to 0.5 and 2.0 to 1.0.
        var severity = Score("severity", 1.0);
        var frustration = Score("frustration", 2.0);

        // Each answer is normalised by its own rubric length first, so rubrics of different lengths
        // combine meaningfully. severity is 1/2, frustration is 2/2.
        var combined = CompositeScore.Weighted(
            new WeightedScore(severity, 1),
            new WeightedScore(frustration, 1));

        Assert.Equal(0.75, combined, precision: 10);

        // Weights are relative, so shifting importance moves the result without any new inference.
        var severityLed = CompositeScore.Weighted(
            new WeightedScore(severity, 3),
            new WeightedScore(frustration, 1));

        Assert.Equal(0.625, severityLed, precision: 10);
    }

    [Fact]
    public void CompositeScoresRejectAnEmptyOrWeightlessInput()
    {
        Assert.Throws<ArgumentException>(() => CompositeScore.Weighted());

        var answer = Score("a", 1);
        Assert.Throws<ArgumentException>(() => CompositeScore.Weighted(new WeightedScore(answer, 0)));
        Assert.Throws<ArgumentException>(() => CompositeScore.Weighted(new WeightedScore(answer, -1)));
    }

    [Fact]
    public void CompositeScoresCanBeEvaluatedAgainstSeveralWeightingProfiles()
    {
        var severity = Score("severity", 1.0);
        var frustration = Score("frustration", 2.0);

        var profiles = CompositeScore.Profiles(
            [new WeightedScore(severity, 0), new WeightedScore(frustration, 0)],
            new Dictionary<string, IReadOnlyDictionary<string, double>>
            {
                ["engineer"] = new Dictionary<string, double> { ["severity"] = 3, ["frustration"] = 1 },
                ["support"] = new Dictionary<string, double> { ["severity"] = 1, ["frustration"] = 3 },
            });

        Assert.Equal(0.625, profiles["engineer"], precision: 10);
        Assert.Equal(0.875, profiles["support"], precision: 10);
    }

    /// <summary>
    /// Builds a three-level score answer with the given score and a flat distribution.
    /// </summary>
    private static ScoreAnswer Score(string id, double score) =>
        new(
            id,
            score,
            new Dictionary<int, System.Text.Json.Nodes.JsonNode?>
            {
                [0] = System.Text.Json.Nodes.JsonValue.Create("low"),
                [1] = System.Text.Json.Nodes.JsonValue.Create("medium"),
                [2] = System.Text.Json.Nodes.JsonValue.Create("high"),
            },
            new Dictionary<int, double> { [0] = 0.2, [1] = 0.6, [2] = 0.2 },
            confidence: 0.5);
}
