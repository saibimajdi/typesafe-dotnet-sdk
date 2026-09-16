#!/usr/bin/env bash
#
# Rehearse a release locally: restore, build, test, pack, and check that the packages carry the
# version the repository state implies.
#
# This is the same sequence .github/workflows/release.yml runs, minus the publish steps, so a
# failure here is a failure that would have happened after a tag was already pushed. Run it before
# tagging.
#
# Usage:
#   eng/pack.sh
#
# Environment:
#   CONFIGURATION   Build configuration. Default: Release
#   OUTPUT          Package output directory. Default: artifacts
#   FRAMEWORK       Test a single target framework, for example net10.0. Unset by default, which
#                   tests both. Useful on a machine that has only one of the two runtimes
#                   installed; CI always tests both.
#
# Exit codes:
#   0   everything built, tested, packed, and verified
#   1   a step failed, or an expected package was not produced

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

configuration="${CONFIGURATION:-Release}"
output="${OUTPUT:-artifacts}"
solution="TypeSafe.slnx"

# The test project multi-targets net8.0;net10.0 so both shipped assets are executed, not merely
# compiled. Microsoft.Testing.Platform runs every framework it built, and a framework whose runtime
# is missing fails the whole run. Microsoft.Testing.Platform has no VSTest fallback on .NET 10, so
# rather than fail confusingly, narrow the run to a framework whose runtime is actually installed
# and say so. CI installs both runtimes, so this only ever triggers on a local machine.
framework="${FRAMEWORK:-}"
if [[ -z "$framework" ]] && ! dotnet --list-runtimes 2>/dev/null | grep -q '^Microsoft\.NETCore\.App 8\.'; then
    framework="net10.0"
    printf 'note: the .NET 8 runtime is not installed, so the tests will run on net10.0 only.\n'
    printf '      Install the .NET 8 runtime, or set FRAMEWORK=net8.0, to exercise the net8.0 asset.\n'
fi

framework_args=()
if [[ -n "$framework" ]]; then
    framework_args=(--framework "$framework")
fi

step() {
    printf '\n=== %s\n' "$1"
}

step "Reading the version MinVer derives from this repository state"
# MinVer computes the version inside a target, so the target has to run: asking for the property
# without -target:MinVer returns the SDK default of 1.0.0 instead.
version="$(dotnet msbuild src/TypeSafe.Sdk/TypeSafe.Sdk.csproj -target:MinVer -getProperty:PackageVersion -nologo)"
printf 'version: %s\n' "$version"

exact_tag="$(git describe --tags --exact-match 2>/dev/null || true)"
case "$exact_tag" in
    v*)
        expected="${exact_tag#v}"
        if [[ "$version" != "$expected" ]]; then
            printf 'error: HEAD is tagged %s but MinVer produced %s.\n' "$exact_tag" "$version" >&2
            printf '       Check that MinVerTagPrefix is still "v" in Directory.Build.props.\n' >&2
            exit 1
        fi
        printf 'HEAD is tagged %s, and the version matches.\n' "$exact_tag"
        ;;
    *)
        printf 'HEAD is not tagged, so this is a development build. Tag v%s to release it.\n' "$version"
        ;;
esac

step "Restore"
dotnet restore "$solution"

step "Build ($configuration)"
dotnet build "$solution" -c "$configuration" --no-restore

step "Test ($configuration${framework:+, $framework})"
# Microsoft.Testing.Platform syntax: the solution is passed with --solution, never positionally.
dotnet test --solution "$solution" -c "$configuration" --no-build \
    "${framework_args[@]}" \
    --coverage --coverage-output-format cobertura --results-directory ./TestResults

step "Pack ($configuration)"
dotnet pack "$solution" -c "$configuration" --no-build -o "$output"

step "Verify the packages"
status=0
for project in TypeSafe.Sdk TypeSafe.Sdk.DependencyInjection; do
    for extension in nupkg snupkg; do
        file="$output/$project.$version.$extension"
        if [[ -f "$file" ]]; then
            printf '  ok   %s\n' "$file"
        else
            printf '  MISSING  %s\n' "$file" >&2
            status=1
        fi
    done
done

if [[ $status -ne 0 ]]; then
    printf '\nPackages actually produced:\n' >&2
    ls -l "$output" >&2 || true
    exit 1
fi

printf '\nReady: %s contains both packages as .nupkg and .snupkg.\n' "$output"
printf 'Publish with a tag:  git tag v%s && git push origin v%s\n' "$version" "$version"
