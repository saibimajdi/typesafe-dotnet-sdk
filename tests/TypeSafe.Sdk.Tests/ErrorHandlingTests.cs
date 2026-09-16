using System.Net;

namespace TypeSafe.Tests;

/// <summary>
/// Verifies that an unsuccessful response becomes the right exception, carrying the right data.
/// </summary>
public sealed class ErrorHandlingTests
{
    private static TypeSafeClientOptions NoRetry() =>
        new() { Retry = RetryPolicy.None };

    [Fact]
    public async Task AuthenticationFailureBecomesTheAuthenticationException()
    {
        var (client, _) = TestClient.Returning(
            Fixtures.ApplicationErrorBody,
            HttpStatusCode.Unauthorized,
            NoRetry());

        var exception = await Assert.ThrowsAsync<TypeSafeAuthenticationException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("authentication_error", exception.ErrorType);
        Assert.Contains("Cannot authenticate", exception.ErrorMessage!, StringComparison.Ordinal);
        Assert.Contains("authentication_error", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, typeof(TypeSafeBadRequestException))]
    [InlineData(HttpStatusCode.Unauthorized, typeof(TypeSafeAuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(TypeSafePermissionDeniedException))]
    [InlineData(HttpStatusCode.NotFound, typeof(TypeSafeNotFoundException))]
    [InlineData(HttpStatusCode.UnprocessableEntity, typeof(TypeSafeUnprocessableEntityException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(TypeSafeRateLimitException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(TypeSafeServerException))]
    [InlineData((HttpStatusCode)529, typeof(TypeSafeServerException))]
    public async Task EachStatusMapsToItsDocumentedExceptionType(HttpStatusCode statusCode, Type expected)
    {
        var (client, _) = TestClient.Returning(
            Fixtures.ApplicationErrorBody,
            statusCode,
            new TypeSafeClientOptions { Retry = RetryPolicy.None });

        var exception = await Assert.ThrowsAnyAsync<TypeSafeApiException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        // The concrete type is chosen from the status code alone, because the status code is
        // documented and stable while error_type is neither.
        Assert.IsType(expected, exception);
        Assert.Equal(statusCode, exception.StatusCode);
    }

    [Fact]
    public async Task AFrameworkErrorWithABareStringDetailIsHandled()
    {
        var (client, _) = TestClient.Returning(Fixtures.FrameworkErrorBody, HttpStatusCode.NotFound, NoRetry());

        var exception = await Assert.ThrowsAsync<TypeSafeNotFoundException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        // The API's detail member is polymorphic: an object for application errors and a bare
        // string for framework errors.
        Assert.Equal("Not Found", exception.ErrorMessage);
        Assert.Null(exception.ErrorType);
    }

    [Fact]
    public async Task ANonJsonErrorBodyIsPreservedRatherThanThrown()
    {
        var (client, _) = TestClient.Create(
            (_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent(Fixtures.NonJsonErrorBody, System.Text.Encoding.UTF8, "text/html"),
                };

                response.Headers.TryAddWithoutValidation("x-typesafe-request-id", "req_proxy");
                return response;
            },
            NoRetry());

        var exception = await Assert.ThrowsAsync<TypeSafeServerException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        // Failing to parse an error must never mask the error, so the text is kept as the message.
        Assert.Contains("502 Bad Gateway", exception.ErrorMessage!, StringComparison.Ordinal);
        Assert.Equal("req_proxy", exception.RequestId);
    }

    [Fact]
    public async Task AnEmptyErrorBodyStillProducesAUsefulMessage()
    {
        var (client, _) = TestClient.Create(
            (_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(string.Empty) },
            NoRetry());

        var exception = await Assert.ThrowsAsync<TypeSafeServerException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        Assert.Contains("500", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApiExceptionsCarryTheRequestIdEndpointAndBody()
    {
        var (client, _) = TestClient.Returning(
            Fixtures.ApplicationErrorBody,
            HttpStatusCode.Unauthorized,
            NoRetry(),
            requestId: "req_01a0ab8265a27733a1bc672bfe98d906");

        var exception = await Assert.ThrowsAsync<TypeSafeAuthenticationException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        Assert.Equal("req_01a0ab8265a27733a1bc672bfe98d906", exception.RequestId);
        Assert.Contains(exception.RequestId!, exception.Message, StringComparison.Ordinal);
        Assert.Equal("POST https://api.typesafe.ai/v1/systemone", exception.Endpoint);
        Assert.NotNull(exception.Body);
        Assert.Equal("https://docs.typesafe.ai/api", exception.DocumentationUrl);
        Assert.True(exception.Headers.ContainsKey("content-type"), "The response headers were not captured.");
    }

    [Fact]
    public async Task ARateLimitExceptionCarriesTheServerRequestedDelay()
    {
        var (client, _) = TestClient.Create(
            (_, _) =>
            {
                var response = StubHttpMessageHandler.Json(
                    """{"detail":{"error_type":"rate_limit_error","message":"Too many requests."}}""",
                    HttpStatusCode.TooManyRequests);

                response.Headers.TryAddWithoutValidation("Retry-After", "12");
                return response;
            },
            NoRetry());

        var exception = await Assert.ThrowsAsync<TypeSafeRateLimitException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        // The SDK retries 429s itself; seeing the exception means the retries were exhausted, so the
        // caller needs to know how long to wait.
        Assert.Equal(TimeSpan.FromSeconds(12), exception.RetryAfter);
    }

    [Fact]
    public async Task AnHttpDateRetryAfterIsUnderstood()
    {
        var when = DateTimeOffset.UtcNow.AddSeconds(30);

        var (client, _) = TestClient.Create(
            (_, _) =>
            {
                var response = StubHttpMessageHandler.Json("""{"detail":"slow down"}""", HttpStatusCode.TooManyRequests);
                response.Headers.TryAddWithoutValidation("Retry-After", when.ToString("R"));
                return response;
            },
            NoRetry());

        var exception = await Assert.ThrowsAsync<TypeSafeRateLimitException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        Assert.NotNull(exception.RetryAfter);
        Assert.InRange(exception.RetryAfter!.Value.TotalSeconds, 25, 31);
    }

    [Fact]
    public async Task EverySdkExceptionDerivesFromTheCommonBase()
    {
        var (client, _) = TestClient.Returning(Fixtures.ApplicationErrorBody, HttpStatusCode.Unauthorized, NoRetry());

        // One catch clause is enough to handle any SDK failure.
        var caught = await Assert.ThrowsAnyAsync<TypeSafeException>(
            () => client.SystemOneAsync("text", [new NoulQuestion("a", "q?")], TestContext.Current.CancellationToken));

        Assert.IsAssignableFrom<TypeSafeApiException>(caught);
    }

    [Fact]
    public async Task AMissingApiKeyDoesNotReachTheNetwork()
    {
        using var environment = new EnvironmentScope()
            .Set(TypeSafeDefaults.ApiKeyEnvironmentVariable, null)
            .Set(TypeSafeDefaults.BaseUrlEnvironmentVariable, null);

        var handler = new StubHttpMessageHandler(StubHttpMessageHandler.Json(Fixtures.NoulResponse));

        // Configuration errors are raised before any request is built.
        Assert.Throws<TypeSafeConfigurationException>(() => new TypeSafeClient(options: null, new HttpClient(handler, disposeHandler: false)));
        Assert.Equal(0, handler.Attempts);

        await Task.CompletedTask;
    }
}
