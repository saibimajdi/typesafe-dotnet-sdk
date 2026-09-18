# Security Policy

## Scope

This policy covers the code in this repository: the `TypeSafeAI.Sdk` and
`TypeSafeAI.Sdk.DependencyInjection` packages, the build and release workflows that publish them,
and the documentation in `docs/`.

It does **not** cover the TypeSafe AI service or API. This is an independent, community
maintained client, and vulnerabilities in the API, the models, the console, or the account
system are TypeSafe AI's to handle — report those to TypeSafe AI directly. What this project
owns is how the SDK handles your credentials, your data, and your HTTP traffic on the client
side.

## Supported versions

This project is pre-1.0. Only the most recent release line receives security fixes.

| Version | Supported |
| --- | --- |
| 0.1.x (latest 0.1 release) | :white_check_mark: |
| Anything older than the latest release | :x: |

Once a 1.0 line exists, this table will list each supported major line and its support window.
Until then, the fix for a security issue is a new release on the latest line, and the upgrade
path is to the newest version. Practically: if you are on `0.1.0` and `0.1.1` exists, `0.1.1`
is the supported version.

## Reporting a vulnerability

**Report privately through
[GitHub Security Advisories](https://github.com/saibimajdi/typesafeai-dotnet-sdk/security/advisories/new).**
That opens a private thread visible only to you and the maintainers, and it is the fastest
route to a fix.

Please do not open a public issue, pull request, or discussion for a suspected
vulnerability, and do not disclose it publicly until a fix has been released and you have been
told it is safe to do so.

A useful report includes:

- The affected package and version, and the target framework you are on.
- What the vulnerability is and what an attacker gains from it.
- A minimal reproduction: the smallest program, request, or configuration that demonstrates it.
- Whether the issue is already public anywhere, and any suggested fix or mitigation.

If you would rather not use GitHub, open a **minimal** public issue that says only that you
have a security report and asks for a private channel — no details, no reproduction, no
version numbers. A maintainer will follow up with a private route.

### What to expect

| Stage | Target |
| --- | --- |
| Acknowledgement of your report | Within 5 business days |
| Initial assessment, including whether we consider it in scope | Within 10 business days |
| Fix or documented mitigation for a confirmed issue | Within 90 days of the report |
| Public advisory and credit | After the fixed release is published, coordinated with you |

We will keep you updated as the assessment progresses, and we will credit you in the advisory
unless you ask us not to.

## What is safe to include in a public issue

Most bug reports need none of the sensitive material below, and a report that includes a live
API key is a credential leak even if the bug is trivial.

**Never put in a public issue, pull request, discussion, or log paste:**

- Your TypeSafe API key, in any form. Not the key, not a prefix of it, not a "redacted" key
  with the middle removed.
- `Authorization` headers, cookies, or any other credential-bearing header, from the SDK or
  from your own proxy.
- Personal data, customer records, or proprietary text that you fed to the API as `state`.
  Reduce the state to a synthetic example that reproduces the problem.
- A full `HttpClient` trace or debug log without reading it first.

**Safe to include in a public issue:**

- **The `x-typesafe-request-id` value.** It identifies one request to TypeSafe support and is
  not a credential. It is exposed as `SystemOneResult.RequestId` and as
  `TypeSafeApiException.RequestId`. Support will ask for it.
- The SDK version (`TypeSafe.Client` assembly version, or the NuGet package version).
- Your target framework (`net8.0` or `net10.0`) and the .NET runtime version.
- Exception types and messages from the SDK.
- Request and response *shapes* — field names, status codes, counts — with the values
  replaced by placeholders.

If you have already pasted a key by accident, revoke it in the
[TypeSafe console](https://console.typesafe.ai/) and create a new one. Revoking is the only
remedy; assume a key that has been public is compromised.

## How the SDK handles credentials

The SDK is built so that ordinary debugging does not turn into a credential leak.

- **The API key is sent only in the `Authorization` request header**, as
  `Authorization: Bearer <key>`.
- **The SDK's own debug logging masks credential-shaped headers.** Any request header whose
  name contains `authorization`, `cookie`, `token`, `secret`, or `key` is logged as `***`. The
  masking is applied only on the debug-logging path, so it cannot be skipped by enabling
  logging at a different level.
- **The dependency injection package stops the framework from logging the key.**
  `IHttpClientFactory`'s default handler logs every request header at `Trace` level, and on
  `Microsoft.Extensions.Http` 8.x that line includes the `Authorization` header verbatim.
  `AddTypeSafeClient` therefore calls `RedactLoggedHeaders` for the named client, so a
  consumer who turns on `Trace` logging does not write live credentials to their logs.
- **`TypeSafeApiException.Endpoint` never contains credentials**, query parameters, or a
  fragment, so it is safe to log and to paste into an issue.
- **Request and response bodies are not redacted.** The SDK logs at the header level and does
  not rewrite the state you sent. If your `state` contains secrets, the body is your
  responsibility: do not enable `Trace`-level logging of bodies in an environment where those
  logs are shipped somewhere you would not ship the data itself, and sanitise the state before
  it reaches the API.

The API key is read from an explicit option or from `TYPESAFE_API_KEY`, and is never written
back out by the SDK. Do not commit it to source control; use your platform's secret store or
environment configuration.

## Dependencies

Runtime dependencies are kept deliberately small, and both of the SDK's own packages are built
with trimming and ahead-of-time compilation analyzers enabled, so the shipped code is free of
reflection that a linker could silently break. Dependabot watches the NuGet and GitHub Actions
dependencies weekly, and every pull request runs a dependency review that fails on a known
high-severity advisory in a newly introduced dependency.
