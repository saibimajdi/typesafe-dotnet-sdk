namespace TypeSafeAI.BureauOfBadIdeas;

internal sealed class BureauConsole(bool color, bool animate)
{
    private const int MeterWidth = 34;
    private readonly bool _color = color && !Console.IsOutputRedirected;
    private readonly bool _animate = animate && !Console.IsOutputRedirected;

    internal void Banner()
    {
        Paint(ConsoleColor.Magenta, "╔══════════════════════════════════════════════════════════════════════════╗");
        Paint(ConsoleColor.Magenta, "║        DEPARTMENT OF QUESTIONABLE INITIATIVES — FORM 13-B               ║");
        Paint(ConsoleColor.Magenta, "║                     T H E   B U R E A U                                  ║");
        Paint(ConsoleColor.Magenta, "║                       O F   B A D   I D E A S                            ║");
        Paint(ConsoleColor.Magenta, "╚══════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    internal async Task ProcessingAsync(bool simulated)
    {
        var stages = simulated
            ? new[] { "Waking the demo pigeon", "Rubber-stamping imaginary paperwork", "Consulting local raccoon" }
            : new[] { "Sending eight typed judgments in one request", "Measuring probability-shaped smoke", "Laminating the verdict" };

        foreach (var stage in stages)
        {
            Paint(ConsoleColor.DarkGray, $"  ◌ {stage}...");
            if (_animate)
            {
                await Task.Delay(220);
            }
        }

        Console.WriteLine();
    }

    internal void Report(string proposal, BureauReport report, BureauVerdict verdict, bool simulated)
    {
        Heading("INTAKE");
        Console.WriteLine($"  Proposal : {proposal}");
        Console.WriteLine($"  Analyst  : {report.Model}");
        Console.WriteLine($"  Mode     : {(simulated ? "DEMO — locally simulated answers" : "LIVE — TypeSafe System One")}");

        Heading("TYPED JUDGMENTS");
        KeyValue("Taxonomy", Humanize(report.Category.Label), report.Category.Confidence);
        PrintTop(report.Category);
        KeyValue("Spirit animal", Humanize(report.Mascot.Label), report.Mascot.Confidence);
        PrintTop(report.Mascot);
        Meter("Operational chaos", report.Chaos.NormalizedScore, ConsoleColor.Red);
        Meter("Spectacle yield", report.Spectacle.NormalizedScore, ConsoleColor.Cyan);
        Meter("Executive energy", report.ExecutiveEnergy.NormalizedScore, ConsoleColor.Yellow);

        Heading("SPECIALIZED INSTRUMENTS");
        Probability("Formal waiver indicated", report.RequiresWaiver.Probability);
        Probability("Probably haunted", report.ProbablyHaunted.Probability);
        Probability("Could have been an email", report.CouldHaveBeenEmail.Probability);
        Meter("COMPOSITE LUNACY INDEX", verdict.LunacyIndex, ConsoleColor.Magenta);

        Heading("OFFICIAL DISPOSITION");
        Paint(ConsoleColor.White, "  ┌──────────────────────────────────────────────────────────────────────┐");
        Paint(ConsoleColor.Green, $"  │ {Fit(verdict.Stamp, 68)} │");
        Paint(ConsoleColor.White, "  └──────────────────────────────────────────────────────────────────────┘");
        Console.WriteLine($"  Clearance : {verdict.Clearance}");
        Console.WriteLine($"  Sponsor   : {verdict.Sponsor}");
        Console.WriteLine($"  Mascot    : {Humanize(report.Mascot.Label)}, {verdict.MascotTitle}");

        Heading("AUTOMATICALLY ASSEMBLED MEMO");
        WriteWrapped(verdict.Memo, 2, 72);
        Console.WriteLine();
        Paint(ConsoleColor.DarkYellow, $"  ⚠ {verdict.FinePrint}");

        if (report.Usage is { } usage)
        {
            Console.WriteLine();
            Paint(ConsoleColor.DarkGray, $"  API usage: {usage.InputTokens} input / {usage.OutputTokens} output tokens");
        }

        Console.WriteLine();
        Paint(ConsoleColor.DarkGray, "  FILED UNDER: ‘What if common sense had an API?’");
    }

    internal void Note(string text) => Paint(ConsoleColor.DarkYellow, $"  {text}");

    internal void Error(string text) => Paint(ConsoleColor.Red, $"  ERROR: {text}");

    internal static void Help()
    {
        Console.WriteLine("Bureau of Bad Ideas — typed AI judgment as terminal comedy\n");
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project examples/TypeSafe.BureauOfBadIdeas -- [options] [proposal]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --demo       Use deterministic local answers even when TYPESAFE_API_KEY is set");
        Console.WriteLine("  --no-anim    Skip the tiny bureaucratic loading ceremony");
        Console.WriteLine("  --no-color   Disable terminal colors");
        Console.WriteLine("  --help       Show this help");
        Console.WriteLine();
        Console.WriteLine("With TYPESAFE_API_KEY set, proposals are judged live. Without it, the Bureau");
        Console.WriteLine("automatically enters clearly labeled demo mode so the example always runs.");
    }

    private void Heading(string text)
    {
        Console.WriteLine();
        Paint(ConsoleColor.Blue, $"── {text} {new string('─', Math.Max(1, 70 - text.Length))}");
    }

    private void KeyValue(string key, string value, double confidence)
    {
        Console.Write($"  {key,-20} ");
        Paint(ConsoleColor.White, value, newline: false);
        Paint(ConsoleColor.DarkGray, $"  (confidence {confidence:P0})");
    }

    private void PrintTop(ChoiceAnswer answer)
    {
        var summary = string.Join("  ·  ", answer.Top(3).Select(pair => $"{Humanize(pair.Key)} {pair.Value:P0}"));
        Paint(ConsoleColor.DarkGray, $"  {"runner-up paperwork",-20} {summary}");
    }

    private void Probability(string label, double probability)
    {
        var color = probability switch
        {
            >= 0.75 => ConsoleColor.Red,
            >= 0.45 => ConsoleColor.Yellow,
            _ => ConsoleColor.Green,
        };

        Console.Write($"  {label,-29} ");
        Paint(color, $"{probability,6:P0}");
    }

    private void Meter(string label, double value, ConsoleColor color)
    {
        var clamped = Math.Clamp(value, 0, 1);
        var filled = (int)Math.Round(clamped * MeterWidth, MidpointRounding.AwayFromZero);
        var bar = new string('█', filled) + new string('░', MeterWidth - filled);
        Console.Write($"  {label,-24} ");
        Paint(color, bar, newline: false);
        Console.WriteLine($" {clamped,6:P0}");
    }

    private void Paint(ConsoleColor color, string text, bool newline = true)
    {
        var previous = Console.ForegroundColor;
        if (_color)
        {
            Console.ForegroundColor = color;
        }

        if (newline)
        {
            Console.WriteLine(text);
        }
        else
        {
            Console.Write(text);
        }

        if (_color)
        {
            Console.ForegroundColor = previous;
        }
    }

    private static string Humanize(string value) =>
        string.Join(' ', value.Split('_').Select(static word =>
            word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));

    private static string Fit(string value, int width) =>
        value.Length <= width ? value.PadRight(width) : string.Concat(value.AsSpan(0, width - 1), "…");

    private static void WriteWrapped(string text, int indent, int width)
    {
        var prefix = new string(' ', indent);
        var line = prefix;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length + word.Length + 1 > width)
            {
                Console.WriteLine(line);
                line = prefix + word;
            }
            else
            {
                line += (line.Length == prefix.Length ? string.Empty : " ") + word;
            }
        }

        if (line.Length > prefix.Length)
        {
            Console.WriteLine(line);
        }
    }
}
