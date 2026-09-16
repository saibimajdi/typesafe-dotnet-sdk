# Releasing

How to publish a version of this SDK to [nuget.org](https://www.nuget.org/).

Publishing uses **NuGet Trusted Publishing**, so no long-lived NuGet API key is stored anywhere.
GitHub mints a short-lived OIDC token, nuget.org validates it against a policy you configure once,
and exchanges it for a temporary API key that is valid for one hour.

## One-time setup

### 1. Create a nuget.org account

Sign in at [nuget.org](https://www.nuget.org/) and note your **profile name** — the part in the URL
at `nuget.org/users/<profile-name>`. This is not your email address.

Store it as a repository secret:

```bash
gh secret set NUGET_USER --body "<your-nuget-profile-name>"
```

### 2. Create the trusted publishing policy

On nuget.org: username menu → **Trusted Publishing** → **Add policy**.

| Field | Value |
| --- | --- |
| Repository Owner | `saibimajdi` |
| Repository | `typesafe-dotnet-sdk` |
| Workflow File | `release.yml` |
| Environment | `nuget` |

> **Workflow File is the file name only.** `release.yml`, not `.github/workflows/release.yml`.
> Getting this wrong produces a policy that never matches, and the failure surfaces as an opaque
> authentication error at publish time rather than as a validation message.

The environment must be `nuget` because `release.yml`'s publish job declares
`environment: nuget`, and the policy is scoped to it. That is what lets the environment's
`v*` deployment tag rule gate every publish.

### 3. Set the policy scopes — the step that is easy to get wrong

Policy scopes control **which packages** the policy may publish, and **whether it may create new
packages** as opposed to new versions of packages that already exist.

For the first release the packages do not exist yet, so the policy **must** permit publishing new
packages. A policy limited to "new versions of existing packages" will reject the very first push.

Tighten the package ID patterns to exactly what this repository ships:

```text
TypeSafe.Sdk
TypeSafe.Sdk.DependencyInjection
```

Do not use a wildcard like `TypeSafe.*`. The policy is the only thing standing between a
compromised workflow run and the ability to publish arbitrary package IDs under your account.

### 4. Publish promptly — the 7-day activation window

> A newly created policy sometimes starts out **temporarily active for 7 days**. If no successful
> publish happens within that window, the policy becomes inactive. The window can be restarted at
> any time.

nuget.org needs the GitHub repository and owner IDs, which it only receives as part of a
successful publish's token, to lock the policy to this repository permanently and prevent
resurrection attacks. Until then the policy is provisional.

**So: create the policy, then release.** If the first attempt fails for an unrelated reason and you
come back in a fortnight, expect to restart the window.

## Releasing a version

### 1. Rehearse

```bash
eng/pack.sh
```

Runs restore, build, test, pack, and package validation locally. A failure here is a failure that
would otherwise have happened after a tag was already pushed.

Then rehearse the workflow itself, which is the only way to exercise the pack-and-verify job
exactly as CI will:

```bash
gh workflow run release.yml -f tag=v0.1.0-alpha.1 -f dry-run=true
```

A dry run builds, tests, packs, and validates, then stops. It never contacts nuget.org and never
creates a GitHub Release.

### 2. Tag and push

```bash
git tag v0.1.0-alpha.1
git push origin v0.1.0-alpha.1
```

The tag is the single source of truth for the version: MinVer derives `0.1.0-alpha.1` from
`v0.1.0-alpha.1`. The workflow fails the run if the produced packages carry anything other than the
tag's version, because a `MinVerTagPrefix` mistake is otherwise silent — every build would quietly
version itself `0.0.0-alpha.0.N`.

### 3. Watch

```bash
gh run watch
```

The publish job gates on the `nuget` environment, which is restricted to `v*` tags.

### 4. Confirm

```bash
gh release view v0.1.0-alpha.1
curl -s -o /dev/null -w '%{http_code}\n' https://api.nuget.org/v3/registration5-gz-semver2/typesafe.sdk/index.json
```

## Version numbering

| Tag | Package version | Meaning |
| --- | --- | --- |
| *(none)* | `0.1.0-alpha.0.<commit-height>` | Development build. Unique per commit, never published. |
| `v0.1.0-alpha.1` | `0.1.0-alpha.1` | Prerelease. Consumers must opt in with `--prerelease`. |
| `v0.1.0` | `0.1.0` | Release. |
| `v1.2.3` | `1.2.3` | Release. |

While the version is `0.x`, a breaking change increments the **minor** version, per the usual
pre-1.0 SemVer convention. `CHANGELOG.md` records what changed.

## Publishing a prerelease first

A published NuGet version can be **unlisted but never deleted**, and the package page is generated
from the embedded `README.md`. Its rendering, the metadata layout, and the dependency graph are
therefore only fully observable after the first successful push.

That is why the first release is a throwaway prerelease: it makes the real package page visible and
correctable before `0.1.0` is frozen. `0.1.0` follows immediately once the page looks right.

## If something goes wrong

| Symptom | Cause |
| --- | --- |
| `403` from nuget.org during push | The policy did not match. Check Workflow File is `release.yml`, that the environment is `nuget`, and that the policy has not passed its 7-day activation window. |
| `409 Conflict` on push | That version already exists. NuGet versions are immutable; publish a new one. |
| Version is `0.0.0-alpha.0.N` | The tag did not match MinVer's prefix. It must start with `v`. |
| Publish job skipped | The `nuget` environment's deployment tag rule allows `v*` only. |
| README links are broken on nuget.org | The README must use absolute URLs. NuGet renders it standalone, so relative paths resolve against nuget.org rather than the repository. |

## Do not

- **Do not add a `NUGET_API_KEY` secret.** Trusted Publishing exists so that no long-lived
  credential for a package publisher is stored in the repository.
- **Do not publish from a laptop.** The policy is bound to this repository's owner and ID, and the
  release workflow is the only path that records provenance in the package.
- **Do not reuse a version.** Unlist and publish a new one.
