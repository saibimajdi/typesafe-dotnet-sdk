# TypeSafe .NET SDK documentation

The [README](../README.md) is the front door and the 60-second quickstart. This directory is the
reference: how each type behaves, what the API guarantees, and how to compose the pieces into
something that survives contact with production traffic.

## Start here

| Document | Read it when |
| --- | --- |
| [quickstart.md](quickstart.md) | You want a working request in five minutes, then configuration, dependency injection, structured state, and error handling. |
| [questions.md](questions.md) | You are choosing between a noul, a choice, and a score, or you have hit one of the documented limits. |
| [answers-and-confidence.md](answers-and-confidence.md) | You are about to put a threshold on an answer, or you are wondering why a noul has no confidence. |
| [patterns.md](patterns.md) | You want to batch questions, gate an action on confidence, combine several scores, or route by intent. |
| [forward-compatibility.md](forward-compatibility.md) | The API has a field, a question kind, or an answer kind the SDK does not model. |
| [retries-and-errors.md](retries-and-errors.md) | A call failed, or you need to reason about timeouts, retries, and budgets before one does. |

## The three ideas worth internalising

**One request, many questions.** Every question in a request sees the same state and is evaluated
independently and in parallel. Adding a question barely changes latency and costs only its tokens.
This is what makes the fan-out pattern in [patterns.md](patterns.md) cheaper than the obvious
alternative of one request per question.

**Answers are typed, and nothing is lost.** Every answer is constrained to the options the question
supplied, so nothing has to be recovered from prose. Anything the SDK does not model is preserved —
`AdditionalProperties` on a question or answer, `RawJson` on a result, `UnknownAnswer` for an answer
kind newer than the SDK, `RawQuestion` for a question kind newer than the SDK. The SDK is never the
reason a new API feature cannot be used.

**Confidence is reported, not invented.** Where the API reports a confidence, the SDK surfaces it
verbatim. Where it does not — a noul — there is no confidence member at all, deliberately, rather
than a value of zero that could be mistaken for a real one. Composing on top, including where to put
a threshold, is caller code. See [answers-and-confidence.md](answers-and-confidence.md).

## Other places to look

- [Contributing guide](../CONTRIBUTING.md) — build, test, coding standards, and how to add a public
  API.
- [Changelog](../CHANGELOG.md) — what shipped when.
- [Support](../SUPPORT.md) — where to ask what, and the boundary between this SDK and the TypeSafe
  service.
- `docs/research/` — background research notes that informed the design. Not user documentation, and
  not maintained as such.
