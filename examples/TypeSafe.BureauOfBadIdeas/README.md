# The Bureau of Bad Ideas

An interactive terminal comedy that turns one ridiculous proposal into eight independent,
typed System One judgments and then lets ordinary C# assemble the verdict.

It is intentionally absurd, but the integration is serious:

- one structured state and one API request;
- two `Choice` questions, three `Score` questions, and three `Noul` questions;
- speculative fan-out: every independent judgment runs together;
- full choice distributions and confidence are visible;
- `CompositeScore.Weighted` combines reusable score answers in code;
- non-compensating safety gates ensure spectacle cannot average away concrete risk;
- deterministic demo mode means the project works in a fresh clone without credentials.

## Run it

From the repository root:

```bash
dotnet run --project examples/TypeSafe.BureauOfBadIdeas
```

That runs the built-in proposal. Supply your own by adding it after `--`:

```bash
dotnet run --project examples/TypeSafe.BureauOfBadIdeas -- \
  "Replace standups with a goose-operated game show and promote whoever survives"
```

For a live judgment, set `TYPESAFE_API_KEY`. Without a key the program automatically uses a
clearly labeled local simulation. Force that behavior with `--demo`:

```bash
dotnet run --project examples/TypeSafe.BureauOfBadIdeas -- \
  --demo --no-anim "Put the roadmap in a glitter cannon and aim it at the moon"
```

Run `--help` for all options.

## Why this is a System One example

Jev does not write the verdict. It supplies small common-sense judgments with typed answers and
probabilities. The application owns the workflow, weights, safety rules, templates, mascot job
titles, and every side effect. That separation is the joke *and* the architecture.

Thresholds in this example are theatrical starting values, not production policy. Evaluate real
thresholds against representative labeled data before using them for consequential decisions.
