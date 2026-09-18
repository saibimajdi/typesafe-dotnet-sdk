# Contributing to the TypeSafe .NET SDK

Thanks for taking the time to contribute. This document covers everything you need to build
the project, the standards the code is held to, and what a pull request is expected to
contain.

By taking part you agree to the [Code of Conduct](CODE_OF_CONDUCT.md). For security issues,
follow [SECURITY.md](SECURITY.md) instead of opening a public issue.

## Ways to contribute

- Report a bug with a minimal reproduction.
- Request a feature, ideally with the shape of the API you would want to call.
- Improve the documentation in [`docs/`](docs/README.md) — a sample that does not compile is a
  bug.
- Add or extend tests.
- Review open pull requests.

## Prerequisites

| Tool | Version | Why |
| --- | --- | --- |
| .NET SDK | 10.0.100 or later | The repository pins this in `global.json`. `rollForward: latestFeature` allows a newer feature band, but not a newer major version. |
| Git | any recent | MinVer derives the package version from git tags, so a git checkout is required. |
| Node.js | 20 or later | Optional. Only needed to run `markdownlint-cli2` locally the way CI does. |

The SDK targets `net8.0` and `net10.0`. You do **not** need a .NET 8 SDK installed: the
.NET 10 SDK restores the `net8.0` reference and targeting packs from NuGet automatically.

You *do* need the .NET 8 **runtime** to execute the `net8.0` tests. The test project
multi-targets `net8.0;net10.0` so that both shipped assets are genuinely exercised rather than
merely compiled, and Microsoft.Testing.Platform runs every target framework it built. With only
the .NET 10 runtime installed you will see this:

```text
Test run summary: Failed!
  error: 1
  total: 146
  failed: 0
  succeeded: 142
  skipped: 4
```

That is one test module failing to start, not a failing test. Either install the .NET 8 runtime,
or restrict the run to the framework you have:

```bash
dotnet test --solution TypeSafe.slnx -f net10.0
```

CI installs both `10.0.x` and `8.0.x` so each leg runs on its own runtime. Do not add
`<RollForward>LatestMajor</RollForward>` to the test project to work around a missing runtime:
that would silently run the `net8.0` assembly on the .NET 10 runtime in CI too, and the
`net8.0` asset would stop being tested for real.

## Getting started

```bash
git clone https://github.com/saibimajdi/typesafeai-dotnet-sdk.git
cd typesafeai-dotnet-sdk
dotnet build TypeSafe.slnx
```

There is no separate one-time install step. Restore happens as part of the first build, and
Central Package Management means every package version already lives in
`Directory.Packages.props`, so there is no tool manifest, workload, or bootstrapper to run.

Every project commits a `packages.lock.json` next to its `.csproj`. Central Package Management
pins the versions this repository names; the lock file pins the whole transitive graph that was
actually restored, and CI restores in locked mode, so a change to a dependency is a change to the
lock file or the build fails with `NU1004`. After changing anything in
`Directory.Packages.props` — or adding a project, which has no lock file until it is restored once
— regenerate them and commit the result:

```bash
dotnet restore TypeSafe.slnx --force-evaluate
```

Build and test:

```bash
dotnet build TypeSafe.slnx
dotnet test --solution TypeSafe.slnx
```

The `--solution` form is required. This repository uses Microsoft.Testing.Platform (opted into
through `global.json`), and in MTP mode `dotnet test` takes `--solution <path>` rather than a
bare positional solution path.

Run the other checks CI runs:

```bash
dotnet format TypeSafe.slnx --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md"
```

Pass a specific project instead of the whole solution when you are iterating:

```bash
dotnet test --project tests/TypeSafe.Sdk.Tests/TypeSafe.Sdk.Tests.csproj -f net10.0
```

The `--project` form is required for the same reason as `--solution`: passing a bare project
path is the VSTest syntax and is rejected under Microsoft.Testing.Platform.

### Coverage

```bash
dotnet test --solution TypeSafe.slnx --coverage
```

Coverage is produced by `Microsoft.Testing.Extensions.CodeCoverage`. coverlet is deliberately
not used anywhere in this repository; do not add it.

CI enforces a floor on it. The thresholds live in the `coverage` job in
[`.github/workflows/ci.yml`](.github/workflows/ci.yml) and are applied by
[`eng/coverage-gate.py`](eng/coverage-gate.py), which you can run over your own run:

```bash
dotnet test --solution TypeSafe.slnx --coverage --coverage-output-format cobertura \
  --results-directory ./TestResults
python3 eng/coverage-gate.py --reports 'TestResults/**/*.cobertura.xml' \
  --line 80 --branch 70 \
  --assembly TypeSafe.Sdk:78:70 \
  --assembly TypeSafe.Sdk.DependencyInjection:90:90
```

The gate is a floor, not a target. It exists so that a change which stops exercising a whole class
of behaviour is caught in review; it deliberately sits a few points below the current numbers
rather than tracking them, because a percentage that has to be nudged upward on every pull request
teaches people to write tests for the number instead of for the behaviour.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/TypeSafe.Sdk/` | The core client. One runtime dependency: `Microsoft.Extensions.Logging.Abstractions`. |
| `src/TypeSafe.Sdk.DependencyInjection/` | `IServiceCollection` registration on top of `IHttpClientFactory`. |
| `tests/TypeSafe.Sdk.Tests/` | xunit.v3 tests, run against both target frameworks. |
| `docs/` | User-facing documentation. Every sample in here must compile. |
| `eng/` | Build and release helpers, including the coverage gate CI runs. |
| `Directory.Build.props` | Compiler and analyzer settings shared by every project. |
| `Directory.Packages.props` | Central Package Management: the single source of every package version. |
| `packages.lock.json` | One per project. The restored transitive graph; regenerate it as described above. |
| `TypeSafe.slnx` | The solution. It is XML, not the legacy `.sln` format. |

## Documentation website

The [documentation site](https://saibimajdi.github.io/typesafeai-dotnet-sdk/) is built with
[Material for MkDocs](https://squidfunk.github.io/mkdocs-material/) from the Markdown files in
`docs/`. Edit those guides directly; `docs/README.md` becomes the site's home page. Add new
guides to the navigation in `mkdocs.yml`. Link to other guides using relative `.md` links;
link to files outside `docs/` using their full GitHub URL so they also work on the site.

Python 3.13 is used in CI. Create a local environment once:

```bash
python -m venv .venv
```

Activate it with `source .venv/bin/activate` on macOS/Linux, or
`.venv\Scripts\Activate.ps1` in PowerShell. Then install and preview:

```bash
python -m pip install -r requirements-docs.txt
python -m mkdocs serve
```

Open the URL printed by MkDocs, normally
`http://127.0.0.1:8000/typesafeai-dotnet-sdk/`. The preview includes the GitHub Pages repository
prefix so relative links behave as they will in production.

Before pushing, run the same build as the Documentation workflow:

```bash
python -m mkdocs build --strict
```

This fails on missing pages, broken internal links, and missing heading anchors. Output goes
to the ignored `site/` directory. Python tooling is only needed for the website; SDK builds
and NuGet consumers are unaffected.

### GitHub Pages setup (maintainers)

Once per repository, open **Settings → Pages → Build and deployment** and select
**GitHub Actions** as the source. Keep the `github-pages` environment restricted to `main`.
No personal access token or additional repository secret is required.

The [Documentation workflow](.github/workflows/docs.yml) builds every pull request without
deployment permissions. After a documentation change merges into `main`, it builds and uploads
the site, then deploys it with GitHub's Pages actions. It can also be run manually on `main`
from the Actions tab, including after first enabling Pages or to retry a failed deployment.
Manual runs on other branches only validate the build.

The published address is `https://saibimajdi.github.io/typesafeai-dotnet-sdk/`. If the repository
is renamed, moved, or given a custom domain, update `site_url` in `mkdocs.yml` and the links
in this guide and the root README. See GitHub's
[custom workflow documentation](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)
for the deployment contract.

## Working with the `.slnx` solution

The solution file is `TypeSafe.slnx`, the XML solution format that `dotnet new sln` produces by
default in .NET SDK 10. It diffs cleanly — no GUIDs and no ordering churn — but a few tooling
caveats are worth knowing before you file a bug against your IDE:

- **Visual Studio does not register itself as the handler for `.slnx`.** Double-clicking the
  file will not open it. Use **File → Open → File…** and pick `TypeSafe.slnx` explicitly, or
  right-click it inside an already-open folder view.
- **Visual Studio 17.14 or later** is needed for `.slnx` support. To produce one from a legacy
  solution, select the solution node in Solution Explorer and use
  **File → Save Solution As… → XML Solution File (\*.SLNX)**.
- **JetBrains Rider 2024.2 or later** supports `.slnx`; older versions do not.
- **VS Code with C# Dev Kit** support is not guaranteed. If the extension does not pick the
  solution up, open the folder — the projects are discovered from the `.csproj` files either
  way — and run the CLI commands above.
- **`dotnet run` against a solution** is not fully supported. Run a project, not the solution.
- **`dotnet package remove` at the repository root** does not work when only a `.slnx` is
  present. Edit the `.csproj` and `Directory.Packages.props` directly.
- **Add projects with the CLI, not by hand:** `dotnet sln TypeSafe.slnx add path/to/Project.csproj`.
  Wildcards are not reliably supported; let the shell expand them if you need several at once.

If any of these blocks you, say so in the pull request — a documented workaround is a
contribution in its own right.

## Coding standards

The compiler and the analyzers are treated as part of the test suite. A warning is a failed
build, so most of the standards below are enforced rather than reviewed.

### Compiler settings

These come from `Directory.Build.props` and `src/Directory.Build.props` and apply to every
shipping project:

- **Nullable reference types are enabled.** Do not use the null-forgiving `!` operator to
  silence a warning; handle the null, or restructure so it cannot occur.
- **Warnings are errors** (`TreatWarningsAsErrors`), including analyzer warnings.
- **Code style is enforced in the build** (`EnforceCodeStyleInBuild`), so `.editorconfig`
  preferences fail the build when they are violated.
- **`AnalysisLevel` is `latest-recommended`**, plus `Meziantou.Analyzer`.
- **XML documentation is required on every public member.** `GenerateDocumentationFile` is on,
  so a missing `<summary>` is `CS1591` and therefore an error. Document the *behaviour* —
  units, ranges, defaults, what a `null` means — not just the signature.
- **Trim and AOT analyzers are on** (`IsTrimmable`, `IsAotCompatible`, `EnableTrimAnalyzer`,
  `EnableAotAnalyzer`, `EnableSingleFileAnalyzer`). Public API that needs reflection must
  either offer a `JsonTypeInfo<T>` overload or be annotated with
  `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` and documented as such.
- **`RS0026` is suppressed on purpose** in `src/Directory.Build.props`, because every overload
  ends in `CancellationToken cancellationToken = default` by convention. Do not "fix" it by
  removing the default; do not add new suppressions without a comment explaining the tradeoff.

### Style

Follow the surrounding code. In particular:

- File-scoped namespaces, `var` when the type is apparent, expression-bodied members for
  one-liners, and `ArgumentNullException.ThrowIfNull` for argument checks.
- Prefer `sealed` on new public classes unless inheritance is a designed feature.
- Fail loudly on programmer error. An invalid question id, an empty option list, or a score
  rubric outside 2–10 levels throws `ArgumentException` locally, before any network call.
- Never silently drop data. A response field the SDK does not model belongs in
  `AdditionalProperties` or `RawJson`.
- Never recompute a value the API reported. `Confidence` is read from the wire, full stop.
- Keep the public surface small. A new public type is a permanent commitment; a new internal
  type is not.
- Adding a dependency needs a discussion first. The core package deliberately has exactly one.

### Formatting

```bash
dotnet format TypeSafe.slnx
```

CI runs `dotnet format TypeSafe.slnx --verify-no-changes`, so run the formatting command above
before pushing.

## Changing the public API

The public surface of each shipping package is declared in that project's
`PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, and enforced by
`Microsoft.CodeAnalysis.PublicApiAnalyzers`. **Adding a public member without updating those
files fails the build with `RS0016`.** Removing or changing one fails with `RS0017`.

To add a public API:

1. Write the member, with complete XML documentation.
2. Build the project.
3. Apply the **RS0016 code fix** to the error — the lightbulb in your IDE, or
   `dotnet build` followed by your editor's "fix all in project". The analyzer prints the
   exact line to add, and the code fix appends it to `PublicAPI.Unshipped.txt` for you.
4. Never hand-write a PublicAPI line from memory. The format is exact — nullability markers
   (`!` for non-null, `?` for nullable), `static`, `abstract`, `override`, and default-value
   spellings such as `= default(System.Threading.CancellationToken)` all matter, and a
   mistyped line is either a build failure or, worse, an inaccurate record.

Then:

- Add tests for the new behaviour, including the failure paths.
- Add an entry to the `## [Unreleased]` section of `CHANGELOG.md`.
- If the change affects how callers use the SDK, update the relevant file under `docs/`, and
  the README if it affects the headline surface.
- Expect a discussion about the shape of the API. Naming and overloads are reviewed closely,
  because they are the parts that cannot be changed later without a major version.

`PublicAPI.Shipped.txt` is updated from `PublicAPI.Unshipped.txt` as part of a release, not by
a contributor, so leave it alone in a feature pull request.

## Commit convention

This repository uses [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
The subject line determines how a change is described in the changelog:

```text
<type>(<optional scope>): <description>

<optional body>

<optional footer>
```

| Type | Use for |
| --- | --- |
| `feat` | A new feature in the SDK. |
| `fix` | A bug fix. |
| `docs` | Documentation only. |
| `test` | Tests only. |
| `build` | Build system, packaging, or dependency changes. |
| `ci` | GitHub Actions and other CI changes. |
| `refactor` | A change that neither fixes a bug nor adds a feature. |
| `perf` | A performance improvement. |
| `chore` | Anything else that does not touch shipped behaviour. |

Rules:

- Write the subject in the imperative mood, lower case, no trailing full stop:
  `fix: honour retry-after-ms before retry-after`.
- Keep the subject under 72 characters.
- Use a scope when it helps: `feat(questions):`, `fix(retry):`, `ci(release):`.
- Mark a breaking change with `!` after the type or scope and explain it in a
  `BREAKING CHANGE:` footer.

Examples:

```text
feat(answers): expose NormalizedScore on ScoreAnswer
fix(retry): clamp the honoured Retry-After to MaxRetryAfter
ci(release): publish with NuGet Trusted Publishing
docs(patterns): correct the composite scoring example
```

## Pull requests

Before you open one:

- [ ] `dotnet build TypeSafe.slnx` succeeds with no warnings.
- [ ] `dotnet test --solution TypeSafe.slnx` passes.
- [ ] `dotnet format TypeSafe.slnx --verify-no-changes` is clean.
- [ ] New behaviour has tests, including the failure paths.
- [ ] Public API changes are reflected in `PublicAPI.Unshipped.txt` via the code fix.
- [ ] `CHANGELOG.md` has an entry under `## [Unreleased]`.
- [ ] Docs and code samples are updated, and every sample you touched compiles.

Expectations:

- **Keep the pull request focused.** One logical change per pull request. Unrelated
  refactoring makes review slower and the change harder to revert.
- **Fill in the pull request template.** The "what" and the "why" both matter; the diff
  already shows the "how".
- **Be explicit about behaviour changes.** If a caller could observe a difference, say so in
  the description, even when the tests pass.
- **CI must be green.** The build and test matrix runs on Linux, Windows, and macOS for both
  target frameworks, plus formatting, markdown linting, the coverage gate, CodeQL, and a
  dependency review.
- **Review is a conversation.** Reviewers may ask for a different API shape or a smaller
  change. That is normal, and it is cheaper now than after a release.

## Releases

Releases are cut by pushing a tag of the form `v0.1.0`. MinVer turns the tag into the package
version, the release workflow builds, tests, packs, verifies that both the `.nupkg` and the
`.snupkg` were produced, publishes to NuGet.org through Trusted Publishing, and creates a
GitHub Release with the packages attached. Maintainers run this process; contributors do not
need to.

## Getting help

- General questions and design discussion:
  [GitHub Discussions](https://github.com/saibimajdi/typesafeai-dotnet-sdk/discussions).
- Bugs and feature requests:
  [GitHub Issues](https://github.com/saibimajdi/typesafeai-dotnet-sdk/issues/new/choose).
- Anything about the TypeSafe API, service, accounts, or billing: see [SUPPORT.md](SUPPORT.md).
