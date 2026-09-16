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

## What is deliberately not here

- **No build bootstrapper.** There is no `build.sh`, no `build.psm1`, no `.config/dotnet-tools.json`,
  and no Cake or Nuke. `dotnet build TypeSafe.slnx` is the whole build, restore is implicit, and
  Central Package Management already pins every package version. A bootstrapper would be a second
  thing to keep working for no gain.
- **No coverage or report scripts.** CI runs `dotnet test --solution TypeSafe.slnx --coverage` and
  uploads the results directory as an artifact. There is no global coverage threshold to enforce,
  deliberately: a global percentage gate rewards tests that execute lines rather than tests that
  check behaviour.
- **No version bumping script.** Versions come from git tags through MinVer. Bumping a version by
  hand is the thing this repository is arranged to make impossible.
- **No release automation beyond the workflow.** Publishing is a tag push, and the workflow does the
  rest, including verifying the produced packages before anything is pushed to nuget.org.

## Possible additions

Each of these is worth adding when it is needed, and not before:

- `eng/aot-smoke/` — a Native AOT publish of a small console app, run nightly. The library is
  annotated and the analyzers are on in every build, so the value is in proving the annotations at
  runtime, not in finding new warnings.
- `eng/verify-packages.sh` — unpacks the produced `.nupkg` files and asserts the dependency
  allow-list and the package metadata, which catches a `PrivateAssets` mistake that a normal build
  would not.
- A `.devcontainer/` for contributors who do not want to install the SDK locally.
