namespace TypeSafeAI.BureauOfBadIdeas;

internal sealed record BureauVerdict(
    string Stamp,
    string Clearance,
    string Sponsor,
    string MascotTitle,
    string Memo,
    string FinePrint,
    double LunacyIndex);

internal static class VerdictEngine
{
    internal static BureauVerdict Decide(string proposal, BureauReport report)
    {
        var lunacy = CompositeScore.Weighted(
            new WeightedScore(report.Chaos, 5),
            new WeightedScore(report.Spectacle, 3),
            new WeightedScore(report.ExecutiveEnergy, 2));

        // Safety is a non-compensating rule: sufficient risk on either independent condition wins.
        // A mountain of entertainment value cannot average away a waiver or an exorcism.
        var dangerous = report.RequiresWaiver.Probability >= 0.78;
        var haunted = report.ProbablyHaunted.Probability >= 0.78;
        var uncertain = report.Category.Confidence < 0.60 || report.Mascot.Confidence < 0.60;

        var (stamp, clearance, finePrint) = (uncertain, dangerous, haunted, lunacy) switch
        {
            (true, _, _, _) => (
                "ESCALATED TO THREE RACCOONS IN A TRENCH COAT",
                "ORANGE / AMBIGUOUS MAMMAL",
                "The Bureau refuses to guess when the typed choice distributions are indecisive."),
            (_, true, _, _) => (
                "REJECTED BY SAFETY; OPTIONED BY A DOCUMENTARY CREW",
                "BLACK-AND-YELLOW / DO NOT LICK",
                "This is comedy, not authorization. Ask the relevant safety, legal, privacy, or security expert."),
            (_, _, true, _) => (
                "QUARANTINED PENDING EXORCISM AND A SECOND OPINION",
                "ECTOPLASM / READ-ONLY",
                "No supernatural side effects may be deployed to production before peer review."),
            (_, _, _, < 0.34) => (
                "DENIED: SUSPICIOUSLY SENSIBLE",
                "BEIGE / NEEDS MORE GEESE",
                "Add one impractical costume, a fog machine, or a measurable amount of hubris and resubmit."),
            (_, _, _, >= 0.78) => (
                "APPROVED FOR ONE (1) GUARDED MOONSHOT",
                "ULTRAVIOLET / EXECUTIVE BLAST RADIUS",
                "Approval covers a miniature, reversible prototype only. The moon has not consented."),
            _ => (
                "RETURNED WITH NOTES: INCREASE GLITTER BY 12%",
                "MAGENTA / CONTROLLED NONSENSE",
                "Promising, but the proposal currently risks being remembered as merely eccentric."),
        };

        var sponsor = report.Category.Label switch
        {
            "operational_shortcut" => "The Ministry of Meetings That Should Have Been Emails",
            "accidental_cult" => "The Office of Voluntary Mandatory Enthusiasm",
            "product_feature" => "The Department of Features Nobody Requested",
            "crime_with_branding" => "Absolutely Not Legal (a fictional consultancy)",
            "performance_art" => "The National Endowment for Confusing the Neighbors",
            _ => "The Miscellaneous Drawer",
        };

        var mascotTitle = report.Mascot.Label switch
        {
            "raccoon" => "CFO (Chief Foraging Officer)",
            "goose" => "VP of Aggressive Alignment",
            "possum" => "Director of Strategic Unavailability",
            "octopus" => "Eight-Armed Program Manager",
            "moth" => "Senior Vice President of Bright Ideas",
            _ => "Vacant Pending Animal Background Check",
        };

        var verb = report.Category.Label switch
        {
            "operational_shortcut" => "streamline",
            "accidental_cult" => "ritualize",
            "product_feature" => "disrupt",
            "crime_with_branding" => "very definitely not implement",
            "performance_art" => "premiere",
            _ => "consider from a respectful distance",
        };

        var memo =
            $"The Bureau proposes to {verb} “{proposal}” under the watchful eye of a " +
            $"{report.Mascot.Label}. Forecast: {report.Chaos.NormalizedScore:P0} chaos, " +
            $"{report.Spectacle.NormalizedScore:P0} spectacle, and {report.ExecutiveEnergy.NormalizedScore:P0} keynote energy. " +
            "All clipboards must be returned before the timeline notices us.";

        return new BureauVerdict(stamp, clearance, sponsor, mascotTitle, memo, finePrint, lunacy);
    }
}
