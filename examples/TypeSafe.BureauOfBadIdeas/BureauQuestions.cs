using System.Text.Json.Nodes;

namespace TypeSafeAI.BureauOfBadIdeas;

internal static class BureauQuestions
{
    internal static readonly ChoiceQuestion Category = new(
        "category",
        "Which single category best describes the proposal in `proposal.text`?",
        new Dictionary<string, string?>
        {
            ["operational_shortcut"] = "An attempt to save time or effort by replacing a normal process with an absurd one.",
            ["accidental_cult"] = "A ritual, belief system, compulsory enthusiasm program, or suspiciously devoted community.",
            ["product_feature"] = "A feature, service, device, or customer experience that could be shipped.",
            ["crime_with_branding"] = "The proposal resembles wrongdoing, trespass, deception, or dangerous conduct dressed up as marketing.",
            ["performance_art"] = "The main value is spectacle, confusion, comedy, or artistic expression.",
            ["other"] = "None of the other categories fits well.",
        });

    internal static readonly ChoiceQuestion Mascot = new(
        "mascot",
        "Which mascot best captures the personality of the proposal in `proposal.text`?",
        new Dictionary<string, string?>
        {
            ["raccoon"] = "Resourceful, nocturnal, trash-adjacent improvisation.",
            ["goose"] = "Loud, territorial, confident, and impossible to negotiate with.",
            ["possum"] = "Survives chaos by looking convincingly unavailable.",
            ["octopus"] = "Complicated, clever, and operating too many levers at once.",
            ["moth"] = "Drawn irresistibly toward a bright and obviously dangerous objective.",
            ["none"] = "The proposal has no meaningful animal energy.",
        });

    internal static readonly ScoreQuestion Chaos = new(
        "chaos",
        "How much operational chaos would the proposal in `proposal.text` create if implemented literally?",
        [
            "Ordinary and controlled; existing procedures could absorb it.",
            "Noticeably weird; a manager would ask one follow-up question.",
            "Disruptive; calendars, furniture, or dignity may need replacing.",
            "Severe; several departments would invent new incident codes.",
            "Visible from space; future historians would name the week after it.",
        ]);

    internal static readonly ScoreQuestion Spectacle = new(
        "spectacle",
        "How entertaining would the visible result of the proposal in `proposal.text` be to an uninvolved observer?",
        [
            "No spectacle; indistinguishable from routine administration.",
            "Mildly amusing; worth one photo in a group chat.",
            "Memorable; strangers would stop and watch.",
            "Extraordinary; local news would arrive without being called.",
            "Legendary; the footage would outlive the organization.",
        ]);

    internal static readonly ScoreQuestion ExecutiveEnergy = new(
        "executive_energy",
        "How strongly does the proposal in `proposal.text` sound like something an overconfident executive might announce on stage?",
        [
            "No executive energy; practical, modest, and specific.",
            "Contains one suspicious buzzword or heroic claim.",
            "Could appear on a keynote slide beside an upward arrow.",
            "Would be announced as a paradigm shift before anyone checks feasibility.",
            "Maximum visionary certainty; the budget has already vanished.",
        ]);

    internal static readonly NoulQuestion RequiresWaiver = new(
        "requires_waiver",
        "Would implementing `proposal.text` literally create a clear physical, legal, financial, privacy, or security risk that warrants expert review or a formal waiver?",
        new NoulCriteria(
            @true: "There is a concrete risk beyond embarrassment or ordinary inconvenience.",
            @false: "The proposal is harmless or only socially awkward."));

    internal static readonly NoulQuestion ProbablyHaunted = new(
        "probably_haunted",
        "Does `proposal.text` contain occult, supernatural, cursed, undead, summoning, possession, or ritual-like elements?",
        new NoulCriteria(
            @true: "The proposal explicitly or strongly implicitly contains a supernatural or ritual element.",
            @false: "No meaningful supernatural or ritual element is present."));

    internal static readonly NoulQuestion CouldHaveBeenEmail = new(
        "could_have_been_email",
        "Is the proposal in `proposal.text` mainly an elaborate replacement for communication or coordination that a short email could accomplish?",
        new NoulCriteria(
            @true: "A short written message would achieve substantially the same practical goal.",
            @false: "The proposal is not mainly a communication or coordination substitute."));

    internal static readonly IReadOnlyList<Question> All =
    [
        Category,
        Mascot,
        Chaos,
        Spectacle,
        ExecutiveEnergy,
        RequiresWaiver,
        ProbablyHaunted,
        CouldHaveBeenEmail,
    ];

    internal static JsonObject StateFor(string proposal) => new()
    {
        ["proposal"] = new JsonObject
        {
            ["text"] = proposal,
            ["submitted_by"] = "an anonymous carbon-based stakeholder",
        },
        ["bureau"] = new JsonObject
        {
            ["mission"] = "Evaluate ridiculous proposals without confusing comedy with permission to do dangerous things.",
            ["operating_assumption"] = "The proposal is hypothetical unless a qualified human explicitly approves it.",
        },
    };
}
