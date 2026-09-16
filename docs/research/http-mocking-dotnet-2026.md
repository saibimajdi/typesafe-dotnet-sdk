# HTTP mocking / faking for a .NET client SDK — research note (September 2026)

Every version, package ID and API name below was fetched on **2026-09-16** from nuget.org's registration/catalog
API, from actual `.nupkg` contents, from GitHub, or from official docs. Unverified items are in
[UNCERTAIN / UNVERIFIED](#uncertain--unverified).

## Comparison table

| Tool | Package ID | Latest (published) | License | In-process? | Real sockets? | Best for | Maintenance |
|---|---|---|---|---|---|---|---|
| Hand-rolled `HttpMessageHandler` | *(your test code)* | n/a | n/a | Yes | No | unit tests: URL building, serialization, error mapping, exact-request asserts | you own it |
| MockHttp | `RichardSzalay.MockHttp` | 7.1.0 (2026-08-08) | MIT | Yes | No | fluent request matching + response queue, no socket | active again (7.0.0 was 2023-10-12) |
| WireMock.Net | `WireMock.Net` | 2.15.0 (2026-08-15) | Apache-2.0 | Yes, but it *is* an HTTP server on a real port | Yes (`server.Urls[0]`) | integration/contract tests, record–replay, faults, OpenAPI | very active (pushed 2026-09-10) |
| WireMock.Net (Docker) | `WireMock.Net.Testcontainers` | 2.15.0 (2026-08-15) | MIT per NuGet (repo is Apache-2.0) | No | Yes | CI parity, non-.NET consumers | very active |
| WireMock.Net glue | `WireMock.Net.xUnit`, `.xUnit.v3`, `.TUnit`, `.NUnit`, `.Aspire`, `.AwesomeAssertions`, `.FluentAssertions` | all 2.15.0 (2026-08-15) | Apache-2.0 (xUnit pkg) | Yes | inherits server | fixtures + assertion sugar | very active |
| TestServer | `Microsoft.AspNetCore.TestHost` | 10.0.12 (2026-09-08); `11.0.0-rc.1.26425.128` preview | MIT | Yes — in-memory, no socket | No | testing a **server you own**; wrong tool for a client SDK | ships with ASP.NET Core |
| WebApplicationFactory | `Microsoft.AspNetCore.Mvc.Testing` | 10.0.12 (2026-09-08) | MIT | Yes | No | booting *your* ASP.NET Core app in tests | ships with ASP.NET Core |
| JustEat.HttpClientInterception | `JustEat.HttpClientInterception` | 5.1.4 (2026-07-30) | Apache-2.0 | Yes | No | JSON-file ("HTTP bundle") stubs, fault injection, DI-shaped interceptor | active (pushed 2026-09-15) |
| NSubstitute | `NSubstitute` | 6.2.0 (2026-08-11) | BSD-3-Clause | Yes | n/a | substituting *your* abstractions | active |
| Moq | `Moq` | 4.20.72 (**2024-09-07**) | BSD-3-Clause | Yes | n/a | same, but no release in ~2 years | low activity (`devlooped/moq`) |

## 1. Hand-rolled `HttpMessageHandler` stub — the default for a small SDK

`HttpClient`'s convenience methods are non-virtual and all funnel into `HttpMessageInvoker.SendAsync`, so mocking
`HttpClient` either fails to intercept or needs `Protected()` reflection — and it bypasses the handler pipeline
(retry/auth/logging), which is exactly what an SDK test should exercise. Per
[How to unit-test code that uses HttpClient (2026-04-26)](https://startdebugging.net/2026/04/how-to-unit-test-code-that-uses-httpclient/):
*"do not mock `HttpClient` itself… The handler is the seam, not the client"* (pattern unchanged on .NET 6/8/9/10/11).
Three pitfalls for the SDK's test guide:

1. **Override `SendAsync(HttpRequestMessage, CancellationToken)`** — the one abstract member ([docs](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler.sendasync?view=net-10.0)).
2. **`Send` is sync-over-async.** `HttpClient.Send` calls `base.Send`, and [`HttpMessageHandler.Send`](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/HttpMessageHandler.cs)
   defaults to `throw new NotSupportedException(SR.net_http_missing_sync_implementation)` (see [`HttpClient.cs`](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/HttpClient.cs)). Override `Send` too if any consumer can take a sync path (MockHttp does, under `#if NET5_0_OR_GREATER`).
3. **Dispose ownership.** `new HttpClient(handler)` equals `HttpClient(handler, true)`, so the handler dies with the client ([ctor docs](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.-ctor?view=net-10.0)). A reused stub handler must be passed as `new HttpClient(handler, disposeHandler: false)`, or the second test gets `ObjectDisposedException`.

```csharp
internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _queue = new();
    public List<HttpRequestMessage> Requests { get; } = [];

    public StubHandler Enqueue(HttpStatusCode status, string json) =>
        Enqueue(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),   // (c) custom status/content
        });

    public StubHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        _queue.Enqueue(factory);                                                    // (b) response queue
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);                                                      // (a) record requests
        if (!_queue.TryDequeue(out var next))
            throw new InvalidOperationException($"No stub for {request.Method} {request.RequestUri}");
        return Task.FromResult(next(request));
    }
}

var handler = new StubHandler()                                    // 503-then-200 for a retry test
    .Enqueue(HttpStatusCode.ServiceUnavailable, """{"error":"busy"}""")
    .Enqueue(HttpStatusCode.OK, """{"id":"42","name":"gear"}""");
var http = new HttpClient(handler, disposeHandler: false)
{
    BaseAddress = new Uri("https://api.example.test"),
};
var widget = await new WidgetClient(http).GetWidgetAsync("42", TestContext.Current.CancellationToken);
Assert.Equal(2, handler.Requests.Count);
Assert.Equal("/v1/widgets/42", handler.Requests[0].RequestUri!.AbsolutePath);
```

**Why it is the right default for a small SDK:** zero dependencies (no added supply-chain surface for consumers), total control of the `HttpResponseMessage` (status, headers, content, streams, `Version`), and direct assertion on the *exact* `HttpRequestMessage` — which is most of what a client SDK's tests are about.

**Testing something that consumes `IHttpClientFactory`.** Register a typed client (`services.AddHttpClient<WidgetClient>(c => c.BaseAddress = new Uri(...))`) and let the SDK ctor take `HttpClient`. Tests then swap the primary handler with `.ConfigurePrimaryHttpMessageHandler(() => new StubHandler())`, or inject a `DelegatingHandler` with `.AddHttpMessageHandler(() => new RecordingHandler())` — both documented on [Use the IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory). Simplest for a library: expose a ctor taking `HttpMessageHandler` and let the DI ctor delegate to it.

## 2. RichardSzalay.MockHttp

`RichardSzalay.MockHttp` **7.1.0**, MIT, published 2026-08-08 ([nuget.org](https://www.nuget.org/packages/RichardSzalay.MockHttp/), [repo](https://github.com/richardszalay/mockhttp), 1.7k stars, not archived). TFMs `netstandard1.1;netstandard2.0;net5.0;net6.0` (verified in `.csproj`/`.nupkg`), so .NET 8/9/10 consumers resolve the `net6.0` asset. **It is not abandoned**: 7.0.0 (2023-10-12) → 7.1.0 (2026-08-08, "Fix support for streaming Content, such as used by Refit"; "Fix for matching content multiple times"), but expect long gaps.

Verified API: `MockHttpMessageHandler`, `When(...)`/`Expect(...)`, matchers `WithQueryString`, `WithJsonContent<T>`, `WithHeaders`, `WithContent`, `.Respond(...)` (overloads for `HttpResponseMessage`, `HttpStatusCode`, `(status, mediaType, content)`, `HttpClient`, `Func<HttpRequestMessage, HttpResponseMessage>`), `.Fallback`, `GetMatchCount(...)`, `VerifyNoOutstandingExpectation()`, `ToHttpClient()`. `When` = repeatable backend definitions; `Expect` = one-shot and order-sensitive; the default fallback throws a match report.

```csharp
var mockHttp = new MockHttpMessageHandler();
var get = mockHttp.When(HttpMethod.Get, "https://api.example.test/v1/widgets/*")
                  .Respond("application/json", """{"id":"42","name":"gear"}""");
mockHttp.When(HttpMethod.Post, "https://api.example.test/v1/widgets")
        .WithJsonContent<CreateWidget>(w => w.Name == "gear")
        .Respond(HttpStatusCode.Created, "application/json", """{"id":"42"}""");
mockHttp.Fallback.Throw(new InvalidOperationException("unexpected request"));

var client = mockHttp.ToHttpClient();                 // or ctor-inject the handler itself
client.BaseAddress = new Uri("https://api.example.test");
await new WidgetClient(client).GetWidgetAsync("42", ct);
Assert.Equal(1, mockHttp.GetMatchCount(get));
```

**Limitations found:**
- **No `IHttpClientFactory` integration — there is no `AddMockHttp` API.** The 7.1.0 `.nuspec` has no `Microsoft.Extensions.*` dependency, and the only extension methods in `MockHttpMessageHandlerExtensions.cs` are `When`/`Expect`. Use `.ConfigurePrimaryHttpMessageHandler(() => mockHttp)`.
- **No HTTP/2 or request-version matcher**: shipped matchers are Method, Url, QueryString, FormData, Content/PartialContent, Headers, Json, Xml, Custom — none for `HttpRequestMessage.Version`/`VersionPolicy`.
- **Sync `Send` *is* supported** (older write-ups say otherwise): `MockHttpMessageHandler.Send` is overridden under `#if NET5_0_OR_GREATER`.

## 3. WireMock.Net — integration/contract tool, not a unit-test tool

All **2.15.0** (2026-08-15): `WireMock.Net` (Apache-2.0; TFMs `net462;net8.0;netstandard2.1`), `.Minimal`, `.xUnit`, `.xUnit.v3`, `.TUnit`, `.NUnit`, `.Aspire`, `.Testcontainers` (NuGet lists MIT), `.OpenApiParser`, `.FluentAssertions`, `.AwesomeAssertions`. Repo [wiremock/WireMock.Net](https://github.com/wiremock/WireMock.Net); docs [wiremock.org/dotnet](https://wiremock.org/dotnet/).

It runs **in-process but not in-memory**: a Kestrel/ASP.NET Core server binding a real port (there is a [KestrelServerOptions page](https://wiremock.org/dotnet/kestrelserveroptions/)), or Docker via `WireMock.Net.Testcontainers` (`sheyenrath/wiremock.net-alpine`). Your SDK therefore speaks real HTTP — catching socket-level behaviour a handler stub cannot (HTTP version, chunking, header casing, connection reuse, timeouts).

```csharp
// usings: WireMock.Server / WireMock.RequestBuilders / WireMock.ResponseBuilders
// (namespaces verified from the shipped XML docs of WireMock.Net.Minimal 2.15.0)
var server = WireMockServer.Start();                       // dynamic port
server.Given(Request.Create().WithPath("/v1/widgets/*").UsingGet())
      .RespondWith(Response.Create()
          .WithStatusCode(200)
          .WithHeader("Content-Type", "application/json")
          .WithBody("""{"id":"42","name":"gear"}"""));
server.Given(Request.Create().WithPath("/v1/widgets/*").UsingGet())   // fault injection
      .AtPriority(1)
      .RespondWith(Response.Create().WithStatusCode(503));

var sdk = new WidgetClient(new HttpClient { BaseAddress = new Uri(server.Urls[0]) });
var hits = server.FindLogEntries(Request.Create().WithPath("/v1/widgets*").UsingGet());  // what the SDK sent
server.Stop();
```

Admin / record–replay: `WireMockServer.StartWithAdminInterface()`; proxying via
`new WireMockServerSettings { StartAdminInterface = true, ProxyAndRecordSettings = new ProxyAndRecordSettings { Url = "https://api.example.test", SaveMapping = true, SaveMappingToFile = true, SaveMappingForStatusCodePattern = "2xx" } }`
— see [Proxying](https://wiremock.org/dotnet/proxying/), [Admin API](https://wiremock.org/dotnet/admin-api-reference/),
[Testcontainers](https://wiremock.org/dotnet/using-wiremock-net-testcontainers/), [unit-test usage](https://wiremock.org/dotnet/using-wiremock-in-unittests/).

**Positioning:** use it for integration/contract tests and to replay recorded upstream behaviour in CI. It is overkill for "does 422 map to `WidgetValidationException`" — that is a 10-line stub handler.

## 4. Microsoft.AspNetCore.TestHost / Mvc.Testing — wrong tool for a *client* SDK

`Microsoft.AspNetCore.TestHost` **10.0.12** (2026-09-08, `net10.0` only) and `Microsoft.AspNetCore.Mvc.Testing` **10.0.12** are MIT and versioned with ASP.NET Core 10; `11.0.0-rc.1.26425.128` is in preview. `TestServer` exposes `CreateClient()` and `CreateHandler()` — an in-memory `HttpMessageHandler` with no socket ([TestServer API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver?view=aspnetcore-10.0)). [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) documents `WebApplicationFactory<TEntryPoint>` and calls `TestServer` the *"in-memory test server"*, supplied by `Microsoft.AspNetCore.Mvc.Testing`.

Why it is wrong here: it tests a **server you own**, and a client SDK has no server. Pointing the SDK at a `TestServer` handler exercises ASP.NET Core's pipeline instead of a network stack — you would be testing your fake. Its legitimate role is the inverse: host a *fake API implementation* with `WebApplicationFactory` when you want a realistic in-process endpoint (though `WireMock.Net` is usually less work unless you want to reuse the real controllers/models).

## 5. JustEat.HttpClientInterception

`JustEat.HttpClientInterception` **5.1.4**, Apache-2.0, 2026-07-30; TFMs `net472;net8.0;netstandard2.0`; repo [justeattakeaway/httpclient-interception](https://github.com/justeattakeaway/httpclient-interception) (pushed 2026-09-15). The ID is `JustEat.*`, **not** `JustEatTakeaway.*` (`justeattakeaway.httpclientinterception` 404s on the flat container). Its README links Microsoft's [IHttpClientFactory guidance](https://learn.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests).

Verified API: `HttpClientInterceptorOptions`, `HttpRequestInterceptionBuilder` (`.Requests().ForGet().ForHttps().ForHost(...).ForPath(...).Responds().WithJsonContent(...)`), `RegisterWith(options)` (extension method in `HttpRequestInterceptionBuilderExtensions.cs`), `options.Register(builder)`, `options.CreateHttpClient()`, `options.CreateHttpMessageHandler()` → `InterceptingHttpMessageHandler : DelegatingHandler` (overrides **both** `Send` and `SendAsync`), JSON "HTTP bundle" files (`RegisterBundle("my-bundle.json")`), and fault injection via `.WithStatus(...)`. **Correction: there is no `.RegisterRequestFor()` method** in the current API. The recommended DI approach is an `IHttpMessageHandlerBuilderFilter` that appends `options.CreateHttpMessageHandler()` to `builder.AdditionalHandlers` (the library ships no `AddHttpMessageHandler` extension).

**vs MockHttp:** MockHttp has the nicer matcher DSL and an `Expect` queue; JustEat wins on (a) JSON bundle files other stacks/teammates can read, (b) an interceptor designed to sit at the end of an existing handler pipeline without the app referencing the library, (c) framework-agnostic fault injection. Both are in-process, no-socket, and both need the SDK to accept an injected `HttpClient`/handler.

## 6. NSubstitute vs Moq — and whether you need either

**NSubstitute 6.2.0** (2026-08-11), BSD-3-Clause per NuGet (the repo's `LICENSE.txt` is the 3-clause BSD text; GitHub says "NOASSERTION" only because of the filename), TFMs `net8.0;netstandard2.0`, repo [nsubstitute/NSubstitute](https://github.com/nsubstitute/NSubstitute) (3.0k stars; 6.0.0 on 2026-07-12, 6.1.0 2026-08-08, 6.2.0 2026-08-11). Actively maintained.

**Moq 4.20.72** (2024-09-07 — still latest on 2026-09-16), BSD-3-Clause, repo now [devlooped/moq](https://github.com/devlooped/moq) (`moq/moq` 301-redirects there). No 5.x exists; the changelog has an "Unreleased" section (#1648) but nothing has shipped in ~2 years.

**The SponsorLink incident (August 2023), verified:**
- **2023-08-08** — Moq **4.20.0** depended on **`Devlooped.SponsorLink` 1.0.0** and embedded `analyzers/dotnet/roslyn4.0/Moq.CodeAnalysis.dll`. I confirmed this by extracting the `.nupkg`: 4.20.0 *and* 4.20.1 contain that analyzer, whose DLL contains the string `sponsorlink`. `Devlooped.SponsorLink` 1.0.0 itself ships `buildTransitive/*.props|targets` + `analyzers/dotnet/Devlooped.SponsorLink.dll`.
- **What it did** — per the maintainer's [SponsorLink announcement](https://www.cazzulino.com/sponsorlink.html): the analyzer runs at build time, shells out to `git config --get user.email`, **SHA-256 hashes + Base62-encodes the email**, and sends HTTP `HEAD` requests to Azure Blob storage (later `cdn.devlooped.com`) to look up sponsorship. The plaintext email is not sent by the client, but the identifier is stable and derived from PII.
- **Reaction** — [issue #1372 "Privacy issues with SponsorLink, starting from version 4.20"](https://github.com/devlooped/moq/issues/1372) (2023-08-08): a closed-source obfuscated DLL, "no option to disable this", GDPR objections; plus [#1433](https://github.com/devlooped/moq/issues/1433) (PCI/HIPAA/GDPR, "It can no longer be trusted"), [#1395](https://github.com/devlooped/moq/issues/1395) (demand to purge the collected PII), [#1396](https://github.com/devlooped/moq/issues/1396). A decompile posted in #1372 showed `Process.Start("git", "config --get user.email")` and the endpoint `https://cdn.devlooped.com/sponsorlink`. Reporting: [OpenSourceForU](https://www.opensourceforu.com/2023/08/open-source-sensation-moq-under-fire-for-stealthy-data-collection/), [Sogeti Labs](https://labs.sogeti.com/moq-and-why-open-source-works), [Safeguard retrospective (2026)](https://safeguard.sh/resources/blog/moq-vulnerability-what-happened-and-what-to-do).
- **Response** — 4.20.1 (2023-08-08) kept the dependency; **4.20.2** (2023-08-09) removed it (PR [#1375](https://github.com/devlooped/moq/pull/1375)); in [#1384](https://github.com/devlooped/moq/issues/1384) (2023-08-10) the maintainer open-sourced SponsorLink unobfuscated at [devlooped/SponsorLink](https://github.com/devlooped/SponsorLink) and confirmed it would no longer be bundled. A leftover `Sponsorable` attribute survived until 4.20.72 (PR #1515).
- **`Moq.Analyzers` is *not* the culprit** — `Moq.Analyzers` (rjmurillo/moq.analyzers, 0.4.2) is an unrelated, legitimate community analyzer package.
- **2026 state** — 4.20.72's `Moq.dll` has no `sponsorlink`/`cdn.devlooped.com` strings and the package ships no analyzers: mechanically clean. `Devlooped.SponsorLink` on NuGet is **unlisted** (every version incl. 1.1.0 has `listed: false`), so it cannot arrive transitively; the live artifact is the `dotnet-sponsor` tool (2.0.16). Trust is still a human call — some teams ban Moq ([#1433](https://github.com/devlooped/moq/issues/1433)), NSubstitute was the migration target linked from #1384, and a fork `NexusKrop.Moq` 4.20.100 exists on Codeberg (~500 downloads — not a serious replacement yet).

**Do you need a mocking framework at all?** For the HTTP layer, no: prefer a fake `HttpMessageHandler` over mocking `HttpClient` — non-virtual methods, bypassed pipeline (a green test can hide a broken retry/auth/logging chain), and a `GetAsync`→`Send` refactor breaking tests ([Start Debugging, 2026](https://startdebugging.net/2026/04/how-to-unit-test-code-that-uses-httpclient/)). A mocking framework earns its place only for the SDK's *own* seams (`Substitute.For<ITokenProvider>()`, asserting an `ILogger` call) — and there, NSubstitute 6.2.0 is the lower-drama default.

## 7. Recommendation for an OSS .NET client SDK

| Layer | Use | Why |
|---|---|---|
| (a) Fast unit tests — serialization, URL/query building, headers, error mapping | **Hand-rolled `StubHandler`** + xUnit (+ `AwesomeAssertions` 9.6.0 if you want fluent asserts) | zero deps, exact-request assertions, no socket, milliseconds |
| (b) Retry / backoff / timeout | Stub handler returning a **queue** of `503`/`429`/`HttpRequestException`; `Microsoft.Extensions.TimeProvider.Testing`'s `FakeTimeProvider` if the SDK takes a `TimeProvider`; assert on the cancellation token rather than `HttpClient.Timeout` | deterministic, no wall-clock sleeps, asserts the *sequence* of attempts |
| (c) Full pipeline incl. resilience | **`Microsoft.Extensions.Http.Resilience` 10.10.0** (2026-09-09) over **Polly 8.8.0** (2026-09-14), with `.ConfigurePrimaryHttpMessageHandler(() => stubHandler)` in the test host | the real `DelegatingHandler` chain runs; only the socket is faked, so retry/backoff/timeout are genuinely exercised |
| (c′) Pipeline + real HTTP semantics | **`WireMock.Net` 2.15.0** in-process (`WireMockServer.Start()`); or `RichardSzalay.MockHttp` 7.1.0 if you want no port | catches socket-level mistakes (HTTP version, chunking, headers, connection reuse) |
| (d) Contract tests vs recorded/live API | **`WireMock.Net` 2.15.0** record–replay (`ProxyAndRecordSettings` + `SaveMappingToFile`) with mappings checked in, plus a nightly re-record against the sandbox; add `WireMock.Net.OpenApiParser` 2.15.0 if the API publishes OpenAPI | recorded fixtures are language-neutral, diffable JSON; the live run catches upstream drift |
| Cross-cutting | **NSubstitute 6.2.0** for the SDK's own abstractions; avoid `Moq` in an OSS SDK in 2026 | clean but unreleased ~2 years and still a trust conversation in reviews |

Minimal stack: `xunit` + `AwesomeAssertions` + your own `StubHandler` + `WireMock.Net` for the integration tier
(+ `Microsoft.Extensions.Http.Resilience` if you ship resilience). No mocking framework is required for the HTTP layer.

## UNCERTAIN / UNVERIFIED

- **.NET 11 GA date / whether `11.0.0-rc.1` is final** — I only confirmed `Microsoft.AspNetCore.TestHost
  11.0.0-rc.1.26425.128` exists in the flat-container index; the registration-leaf fetch failed, so no publish date,
  and I read no .NET 11 schedule.
- **WireMock.Net "Kestrel on a real port"** — inferred from the README ("Library can be used in unit tests and
  integration tests"), the existence of a `KestrelServerOptions` docs page, and official snippets using
  `server.Urls[0]`. No official page literally says "Kestrel".
- **`WireMock.Net.Testcontainers` license** — NuGet says MIT, the repo is Apache-2.0; unresolved.
- **`Devlooped.SponsorLink` "unlisted"** — the registration index reports `listed: false` and
  `published: 1900-01-01` for every version; I did not view the nuget.org web banner.
- **Whether the collected SponsorLink data was ever deleted** — issue #1395 demands it; I read only its first
  comments and could not verify the outcome.
- **`HttpMessageHandler.Send` on .NET Framework/Mono** — my `Send` findings come from `dotnet/runtime` `main` and the
  .NET 10 API docs; .NET 5/6 sources were not checked.
- **MockHttp HTTP/2** — stated only as "no version matcher exists in the shipped matcher set" (verified from the
  source tree); I did not test an HTTP/2 request, and a user can hand-build a response with `Version = HttpVersion.Version20`.
- **nuget.org web "last updated" timestamps** can differ from the catalog `published` values I report.
- **"DI without the app referencing the library"** (JustEat) is my paraphrase of that README's introduction, not a quote.
