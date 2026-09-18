using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace TypeSafeAI.BureauOfBadIdeas;

internal sealed record BureauReport(
    ChoiceAnswer Category,
    ChoiceAnswer Mascot,
    ScoreAnswer Chaos,
    ScoreAnswer Spectacle,
    ScoreAnswer ExecutiveEnergy,
    NoulAnswer RequiresWaiver,
    NoulAnswer ProbablyHaunted,
    NoulAnswer CouldHaveBeenEmail,
    string Model,
    Usage? Usage)
{
    internal static BureauReport From(SystemOneResult result) => new(
        result.Get(BureauQuestions.Category),
        result.Get(BureauQuestions.Mascot),
        result.Get(BureauQuestions.Chaos),
        result.Get(BureauQuestions.Spectacle),
        result.Get(BureauQuestions.ExecutiveEnergy),
        result.Get(BureauQuestions.RequiresWaiver),
        result.Get(BureauQuestions.ProbablyHaunted),
        result.Get(BureauQuestions.CouldHaveBeenEmail),
        result.Model,
        result.Usage);

    internal static BureauReport Simulate(string proposal)
    {
        var lower = proposal.ToLowerInvariant();
        var wobble = StableFraction(proposal);

        var category = lower switch
        {
            var text when ContainsAny(text, "steal", "break in", "fraud", "kidnap", "weapon", "crime") => "crime_with_branding",
            var text when ContainsAny(text, "ritual", "worship", "chant", "summon", "sacrifice", "cult") => "accidental_cult",
            var text when ContainsAny(text, "app", "feature", "button", "service", "platform", "ai") => "product_feature",
            var text when ContainsAny(text, "meeting", "standup", "process", "workflow", "quarterly") => "operational_shortcut",
            _ => "performance_art",
        };

        var mascot = lower switch
        {
            var text when text.Contains("raccoon", StringComparison.Ordinal) => "raccoon",
            var text when text.Contains("goose", StringComparison.Ordinal) => "goose",
            var text when ContainsAny(text, "many", "eight", "tentacle", "simultaneous") => "octopus",
            var text when ContainsAny(text, "night", "light", "moon", "fire") => "moth",
            _ when category == "crime_with_branding" => "raccoon",
            _ when category == "operational_shortcut" => "possum",
            _ => "goose",
        };

        var lengthBoost = Math.Min(proposal.Length / 400.0, 0.2);
        var chaos = Clamp(0.46 + lengthBoost + (0.18 * wobble) + (ContainsAny(lower, "all ", "every ", "explode", "launch", "raccoon") ? 0.16 : 0));
        var spectacle = Clamp(0.48 + (0.25 * wobble) + (ContainsAny(lower, "costume", "yodel", "disco", "parade", "cannon", "moon") ? 0.2 : 0));
        var executive = Clamp(0.30 + (0.30 * (1 - wobble)) + (ContainsAny(lower, "synergy", "platform", "ai", "blockchain", "paradigm") ? 0.28 : 0));
        var waiver = Clamp(0.10 + (category == "crime_with_branding" ? 0.75 : 0) + (ContainsAny(lower, "fire", "weapon", "explode", "traffic", "roof") ? 0.55 : 0));
        var haunted = Clamp(0.04 + (category == "accidental_cult" ? 0.86 : 0) + (ContainsAny(lower, "ghost", "demon", "haunt", "curse", "summon") ? 0.5 : 0));
        var email = Clamp(0.10 + (category == "operational_shortcut" ? 0.58 : 0) + (ContainsAny(lower, "meeting", "announce", "update", "status") ? 0.22 : 0));

        return new BureauReport(
            MakeChoice(BureauQuestions.Category, category),
            MakeChoice(BureauQuestions.Mascot, mascot),
            MakeScore(BureauQuestions.Chaos, chaos),
            MakeScore(BureauQuestions.Spectacle, spectacle),
            MakeScore(BureauQuestions.ExecutiveEnergy, executive),
            new NoulAnswer(BureauQuestions.RequiresWaiver.Id, waiver),
            new NoulAnswer(BureauQuestions.ProbablyHaunted.Id, haunted),
            new NoulAnswer(BureauQuestions.CouldHaveBeenEmail.Id, email),
            "demo-pigeon-0.0 (simulated locally)",
            null);
    }

    private static ChoiceAnswer MakeChoice(ChoiceQuestion question, string winner)
    {
        var probabilities = question.Labels.ToDictionary(
            static label => label,
            label => string.Equals(label, winner, StringComparison.Ordinal)
                ? 0.72
                : 0.28 / (question.Labels.Count - 1),
            StringComparer.Ordinal);

        return new ChoiceAnswer(question.Id, winner, probabilities, 0.86);
    }

    private static ScoreAnswer MakeScore(ScoreQuestion question, double normalizedScore)
    {
        var max = question.LevelCount - 1;
        var score = normalizedScore * max;
        var low = (int)Math.Floor(score);
        var high = Math.Min(low + 1, max);
        var probabilities = Enumerable.Range(0, question.LevelCount).ToDictionary(static level => level, static _ => 0.0);

        if (low == high)
        {
            probabilities[low] = 1;
        }
        else
        {
            probabilities[low] = high - score;
            probabilities[high] = score - low;
        }

        var legend = question.Criteria
            .Select((description, index) => new KeyValuePair<int, JsonNode?>(index, description?.DeepClone()))
            .ToDictionary();

        return new ScoreAnswer(question.Id, score, legend, probabilities, 0.84);
    }

    private static bool ContainsAny(string text, params string[] needles) =>
        needles.Any(text.Contains);

    private static double StableFraction(string value)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToUInt32(digest, 0) / (double)uint.MaxValue;
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 0.99);
}
