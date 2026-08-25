namespace ConnectStone.Sdk.Webhooks;

public interface IWebhookAuthenticator
{
    /// <summary>
    /// Validates the incoming request's <c>Authorization</c> header against the credentials
    /// configured on the Pagar.me webhook endpoint. Pass the raw header value (e.g.
    /// <c>"Basic dXNlcjpwYXNz"</c>) or <see langword="null"/> if the header was absent.
    /// </summary>
    bool IsAuthentic(string? authorizationHeaderValue);
}
