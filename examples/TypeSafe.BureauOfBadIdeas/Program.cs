namespace TypeSafeAI.BureauOfBadIdeas;

internal static class Program
{
    private const string DefaultProposal =
        "Replace quarterly planning with competitive yodeling, award the winner a ceremonial raccoon, " +
        "and launch the roadmap from a glitter cannon toward the moon.";

    private static async Task<int> Main(string[] args)
    {
        if (args.Contains("--help", StringComparer.Ordinal))
        {
            BureauConsole.Help();
            return 0;
        }

        var options = args.Where(static arg => arg.StartsWith("--", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        var unknown = options.Except(["--demo", "--no-anim", "--no-color"], StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
        {
            Console.Error.WriteLine($"Unknown option: {string.Join(", ", unknown)}");
            BureauConsole.Help();
            return 2;
        }

        var proposal = string.Join(' ', args.Where(static arg => !arg.StartsWith("--", StringComparison.Ordinal))).Trim();
        if (proposal.Length == 0)
        {
            proposal = DefaultProposal;
        }

        var hasApiKey = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(TypeSafeDefaults.ApiKeyEnvironmentVariable));
        var simulated = options.Contains("--demo") || !hasApiKey;
        var ui = new BureauConsole(!options.Contains("--no-color"), !options.Contains("--no-anim"));

        ui.Banner();
        if (simulated && !options.Contains("--demo"))
        {
            ui.Note($"No {TypeSafeDefaults.ApiKeyEnvironmentVariable} detected; entering demo mode. The pigeon is not a real model.");
            Console.WriteLine();
        }

        try
        {
            BureauReport report;
            if (simulated)
            {
                report = BureauReport.Simulate(proposal);
            }
            else
            {
                using var client = new TypeSafeClient();
                var result = await client.SystemOneAsync(BureauQuestions.StateFor(proposal), BureauQuestions.All);
                report = BureauReport.From(result);
            }

            await ui.ProcessingAsync(simulated);
            var verdict = VerdictEngine.Decide(proposal, report);
            ui.Report(proposal, report, verdict, simulated);
            return 0;
        }
        catch (TypeSafeException exception)
        {
            ui.Error(exception.Message);
            ui.Note("The Bureau has preserved your dignity. Try --demo or inspect the API configuration.");
            return 1;
        }
    }
}
