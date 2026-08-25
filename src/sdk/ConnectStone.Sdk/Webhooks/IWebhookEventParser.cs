namespace ConnectStone.Sdk.Webhooks;

public interface IWebhookEventParser
{
    /// <summary>
    /// Parses a raw webhook request body into a typed event. Returns <see langword="null"/> for a
    /// well-formed payload whose <c>type</c> isn't one of the events this SDK models yet. Treat
    /// that as "acknowledge and ignore", not an error, since Pagar.me may add new event types over
    /// time and a webhook receiver must not fail on ones it doesn't recognize.
    /// </summary>
    ConnectStoneWebhookEvent? Parse(string rawJsonBody);
}
