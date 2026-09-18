# Support

## No SLA

This is a community-maintained open-source project. Support is **best-effort by volunteers**.
There is no service-level agreement, no guaranteed response time, and no commercial support. A
well-scoped report with a minimal reproduction usually gets an answer quickly; a question that
requires reproducing your application does not.

## Where to ask what

| What you have | Where it goes |
| --- | --- |
| A question about how to use the SDK, or a design question | [GitHub Discussions](https://github.com/saibimajdi/typesafeai-dotnet-sdk/discussions) |
| A bug in the SDK, with a reproduction | [Bug report](https://github.com/saibimajdi/typesafeai-dotnet-sdk/issues/new?template=bug_report.yml) |
| An idea for a capability the SDK lacks | [Feature request](https://github.com/saibimajdi/typesafeai-dotnet-sdk/issues/new?template=feature_request.yml), or a Discussion first if the shape is unclear |
| A security vulnerability | **Privately**, per [SECURITY.md](SECURITY.md) — never a public issue |
| A vulnerability in a third-party dependency | The upstream project; Dependabot and the dependency review check will surface it here too |
| A problem with the TypeSafe API, the models, your account, quotas, or billing | **TypeSafe AI**, through [typesafe.ai](https://typesafe.ai/) and the [console](https://console.typesafe.ai/) |
| A question about the Python or JavaScript SDK | The respective SDK's own repository |

The distinction in the last two rows matters. This SDK is an independent, community-maintained
client for the TypeSafe API. It is not affiliated with, endorsed by, or supported by TypeSafe AI.
Maintainers here can help with what the SDK does on your machine — request construction, retry
behaviour, deserialization, exceptions — and cannot help with the service itself, model behaviour,
API keys, or billing.

## Before you ask

1. **Read the documentation.** [docs/README.md](docs/README.md) indexes everything, and the
   [quickstart](docs/quickstart.md) answers most first-day questions.
2. **Search first.** Discussions and issues are both searchable, and a closed issue often holds
   the answer.
3. **Reduce it to a minimal reproduction.** Delete everything from your program that is not
   needed to show the problem, and replace real data with synthetic values. Most reports that sit
   unanswered are waiting on this step.
4. **Include the version and the target framework.** The SDK version, and whether you are on
   `net8.0` or `net10.0`. For an API failure, include the `x-typesafe-request-id`, which is safe
   to share.

## What not to send

- **Do not include your API key**, in any form, in any public venue. Not the key, not a prefix,
  not a masked key. If one has already been posted, revoke it in the
  [TypeSafe console](https://console.typesafe.ai/) — that is the only remedy.
- Do not include customer data or proprietary text that you sent to the API as `state`. Replace
  it with a synthetic example that still reproduces the problem.
- Do not paste a raw HTTP trace without reading it first.

[SECURITY.md](SECURITY.md) covers this in more detail, including what the SDK does to keep
credentials out of its own logs.

## Supported versions

Only the most recent release line receives fixes. [SECURITY.md](SECURITY.md) has the table, and it
also covers how to report a vulnerability and what to expect after you do.

## We do not provide support through

- Comments on closed issues or merged pull requests.
- Direct messages to maintainers.
- Email, except for conduct reports (see [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)) and security
  reports (see [SECURITY.md](SECURITY.md)).

Keeping the conversation in Discussions and Issues is what makes the answers searchable for
everyone who hits the same problem later.
