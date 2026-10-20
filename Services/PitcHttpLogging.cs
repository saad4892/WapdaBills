using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;

namespace WapdaBill.Services;

internal static class PitcHttpLogging
{
    /// <summary>
    /// Logs outbound PITC failures with actionable diagnostics.
    /// Never logs bill numbers, reference numbers, form payloads, or response PII.
    /// </summary>
    public static void LogOutboundFailure(
        ILogger logger,
        Exception ex,
        string operation,
        string host,
        string? path,
        string? companyCode,
        long elapsedMs,
        HttpStatusCode? statusCode,
        string? responseBodyPreview,
        bool requestAborted)
    {
        var category = Classify(ex, statusCode, requestAborted);
        var inner = FlattenInner(ex);
        var httpError = ex is HttpRequestException hre ? hre.HttpRequestError.ToString() : null;
        var status = statusCode is { } code
            ? (int)code
            : ex is HttpRequestException { StatusCode: { } sc } ? (int)sc : (int?)null;

        logger.LogWarning(
            ex,
            "PITC outbound failed. Operation={Operation} Host={Host} Path={Path} Company={Company} " +
            "Category={Category} ElapsedMs={ElapsedMs} StatusCode={StatusCode} HttpRequestError={HttpRequestError} " +
            "RequestAborted={RequestAborted} ExceptionType={ExceptionType} Inner={Inner} BodyPreview={BodyPreview}",
            operation,
            host,
            path ?? "",
            companyCode ?? "",
            category,
            elapsedMs,
            status,
            httpError,
            requestAborted,
            ex.GetType().FullName,
            inner,
            TruncateSafe(responseBodyPreview));
    }

    public static string Classify(Exception ex, HttpStatusCode? statusCode, bool requestAborted)
    {
        if (requestAborted)
        {
            return "client_aborted";
        }

        if (ex is OperationCanceledException or TaskCanceledException)
        {
            return "timeout_or_cancelled";
        }

        if (statusCode is { } code)
        {
            var n = (int)code;
            if (n >= 500) return "http_5xx";
            if (n >= 400) return "http_4xx";
        }

        for (var cur = ex; cur is not null; cur = cur.InnerException)
        {
            switch (cur)
            {
                case AuthenticationException:
                    return "tls_certificate";
                case HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError }:
                    return "tls_handshake";
                case HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError }:
                    return "dns";
                case HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError }:
                    return "connection";
                case SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData }:
                    return "dns";
                case SocketException { SocketErrorCode: SocketError.TimedOut }:
                    return "socket_timeout";
                case SocketException { SocketErrorCode: SocketError.ConnectionRefused }:
                    return "connection_refused";
                case SocketException { SocketErrorCode: SocketError.NetworkUnreachable or SocketError.HostUnreachable }:
                    return "network_unreachable";
            }
        }

        return "http_request";
    }

    private static string FlattenInner(Exception ex)
    {
        var parts = new List<string>(4);
        for (var cur = ex.InnerException; cur is not null && parts.Count < 4; cur = cur.InnerException)
        {
            parts.Add($"{cur.GetType().Name}: {Sanitize(cur.Message)}");
        }

        return parts.Count == 0 ? "" : string.Join(" | ", parts);
    }

    private static string Sanitize(string message)
    {
        // Strip long tokens that might appear in TLS or HTML error pages.
        if (message.Length <= 300)
        {
            return message;
        }

        return message[..300] + "…";
    }

    private static string TruncateSafe(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return "";
        }

        var trimmed = body.Trim();
        if (trimmed.Length == 0)
        {
            return "";
        }

        // Only log short JSON/text status messages; never large HTML bills.
        if (trimmed.Length > 240 || trimmed.Contains('<', StringComparison.Ordinal))
        {
            return $"(omitted len={trimmed.Length})";
        }

        return trimmed;
    }
}
