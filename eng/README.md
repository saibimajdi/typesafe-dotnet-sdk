# eng — build and release helpers

This directory holds the scripts that a maintainer runs by hand. Everything a contributor needs
day to day lives in the root of the repository and in [CONTRIBUTING.md](../CONTRIBUTING.md); nothing
here is required to build or test the SDK.

## `pack.sh`

Rehearses a release locally: it reads the version MinVer derives from the current repository state,
then restores, builds, tests, packs, and verifies that both packages were produced as a `.nupkg` and
a `.snupkg` under exactly that version.

```bash
eng/pack.sh
```

It is the same sequence [`.github/workflows/release.yml`](../.github/workflows/release.yml) runs,
minus the publish steps, which is the point: a failure here is caught before a tag is pushed rather
than after. The version check is the important part. If `HEAD` carries a `v*` tag, the script fails
unless MinVer produced exactly that version, which is what catches the one MinVer misconfiguration
that otherwise fails silently — a `MinVerTagPrefix` that no longer matches the tags, which makes
every release version `0.1.0-alpha.0.<height>` without anything else complaining.

Environment overrides: `CONFIGURATION` (default `Release`) and `OUTPUT` (default `artifacts`).

Requires `bash`, `git`, and the .NET SDK pinned in `global.json`. It works from Git Bash and WSL on
Windows; on Windows without a POSIX shell, run the individual `dotnet` commands from
[CONTRIBUTING.md](../CONTRIBUTING.md) instead — each step is one command, so a separate PowerShell
script would only restate them.

It restores in locked mode, exactly as CI does, so a change to `Directory.Packages.props` whose
`packages.lock.json` files were never regenerated fails here rather than at the tag.

## `coverage-gate.py`

```bash
python3 eng/coverage-gate.py \
  --reports 'TestResults/**/*.cobertura.xml' \
  --line 80 --branch 70 \
  --assembly TypeSafe.Sdk:78:70 \
  --assembly TypeSafe.Sdk.DependencyInjection:90:90
```

Fails (exit 1) when line or branch coverage is below a floor, and prints a markdown table — the
same table CI puts on the run page and posts as the pull request comment. Thresholds are passed in
as arguments rather than read from a file, so the numbers CI enforces are visible in
[`.github/workflows/ci.yml`](../.github/workflows/ci.yml) and nowhere else.

It is a floor, not a target: it catches a change that stops exercising a whole class of behaviour,
and it sits a few points below the current numbers deliberately, because a percentage that has to
be nudged upward every week teaches people to write tests for the number. Two situations are
errors rather than skips, because both look like a gate that passes while measuring nothing: an
assembly named in the thresholds but missing from the report — which is what a rename or a project
that quietly stopped being tested looks like — and finding no reports or no lines at all (exit 2).

It needs nothing but a Python 3 interpreter and the cobertura reports, and it does not recompute
coverage: the per-assembly rates come from the tool that produced the report, and only the totals
are summed across reports so that a multi-targeted test project is measured as one run rather than
as an average of two.

## What is deliberately not here

- **No build bootstrapper.** There is no `build.sh`, no `build.psm1`, no `.config/dotnet-tools.json`,
  and no Cake or Nuke. `dotnet build TypeSafe.slnx` is the whole build, restore is implicit, and
  Central Package Management already pins every package version. A bootstrapper would be a second
  thing to keep working for no gain.
- **No coverage reporting toolchain.** CI collects coverage with
  `Microsoft.Testing.Extensions.CodeCoverage`, uploads the cobertura reports as artifacts, and the
  gate above reads them where they lie. There is no ReportGenerator, no HTML report, and no
  per-class table: the numbers that decide anything are the ones in the gate, and the raw reports
  are one artifact download away for anyone who wants the detail.
- **No version bumping script.** Versions come from git tags through MinVer. Bumping a version by
  hand is the thing this repository is arranged to make impossible.
- **No release automation beyond the workflow.** Publishing is a tag push, and the workflow does the
  rest: it rehearses the push and, on a dry run from a tag, the OIDC handshake; it verifies the
  produced packages before anything reaches nuget.org; and it confirms afterwards that the published
  version installs.

## Possible additions

Each of these is worth adding when it is needed, and not before:

- `eng/aot-smoke/` — a Native AOT publish of a small console app, run nightly. The library is
  annotated and the analyzers are on in every build, so the value is in proving the annotations at
  runtime, not in finding new warnings.
- `eng/verify-packages.sh` — unpacks the produced `.nupkg` files and asserts the dependency
  allow-list and the package metadata, which catches a `PrivateAssets` mistake that a normal build
  would not.
- A `.devcontainer/` for contributors who do not want to install the SDK locally.
