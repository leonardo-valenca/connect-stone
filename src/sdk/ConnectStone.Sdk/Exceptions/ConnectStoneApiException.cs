using System.Net;

namespace ConnectStone.Sdk.Exceptions;

/// <summary>
/// Thrown when the Pagar.me / Connect Stone API returns a non-success status code.
/// </summary>
public class ConnectStoneApiException(HttpStatusCode statusCode, string responseBody, string? apiMessage) : ConnectStoneException(BuildMessage(statusCode, apiMessage))
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>Raw, unparsed response body. Always available even if <see cref="ApiMessage"/> parsing failed.</summary>
    public string ResponseBody { get; } = responseBody;

    /// <summary>Best-effort parsed <c>message</c> field from the error envelope, if present.</summary>
    public string? ApiMessage { get; } = apiMessage;

    private static string BuildMessage(HttpStatusCode statusCode, string? apiMessage) =>
        apiMessage is null
            ? $"Connect Stone API request failed with status {(int)statusCode} ({statusCode})."
            : $"Connect Stone API request failed with status {(int)statusCode} ({statusCode}): {apiMessage}";
}
