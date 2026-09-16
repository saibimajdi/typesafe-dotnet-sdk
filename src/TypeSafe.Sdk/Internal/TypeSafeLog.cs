using Microsoft.Extensions.Logging;

namespace TypeSafe.Internal;

/// <summary>
/// Source-generated log messages.
/// </summary>
/// <remarks>
/// <see cref="LoggerMessageAttribute"/> generates the level check and the message formatting at
/// compile time, which keeps logging allocation-free when the level is disabled — the normal case
/// for a library.
/// </remarks>
internal static partial class TypeSafeLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "TypeSafe {Operation} completed with {StatusCode} in {ElapsedMs} ms (request id {RequestId})")]
    public static partial void RequestCompleted(
        ILogger logger,
        string operation,
        int statusCode,
        double elapsedMs,
        string? requestId);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "TypeSafe {Operation} failed with {StatusCode} after {Attempts} attempt(s), request id {RequestId}: {Message}")]
    public static partial void RequestFailed(
        ILogger logger,
        string operation,
        int statusCode,
        int attempts,
        string? requestId,
        string message);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "TypeSafe {Operation} could not reach the server after {Attempts} attempt(s): {Message}")]
    public static partial void RequestErrored(
        ILogger logger,
        string operation,
        int attempts,
        string message);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Retrying TypeSafe {Operation} in {DelayMs} ms (attempt {Attempt} of {MaxAttempts}) after {Reason}")]
    public static partial void Retrying(
        ILogger logger,
        string operation,
        double delayMs,
        int attempt,
        int maxAttempts,
        string reason);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Debug,
        Message = "TypeSafe {Operation} request body: {Body}")]
    public static partial void RequestBody(ILogger logger, string operation, string body);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "TypeSafe {Operation} response body: {Body}")]
    public static partial void ResponseBody(ILogger logger, string operation, string body);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Warning,
        Message = "Answer {QuestionId} has unrecognised type '{AnswerType}'; it was kept as an UnknownAnswer and its raw body is available. This usually means the TypeSafe API added an answer kind after this SDK release.")]
    public static partial void UnknownAnswerKind(ILogger logger, string questionId, string answerType);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Debug,
        Message = "TypeSafe {Operation} request headers: {Headers}")]
    public static partial void RequestHeaders(ILogger logger, string operation, string headers);
}
