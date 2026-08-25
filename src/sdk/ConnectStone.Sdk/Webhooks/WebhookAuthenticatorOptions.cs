namespace ConnectStone.Sdk.Webhooks;

/// <summary>
/// The Basic-auth credentials you configured on the webhook endpoint in the Pagar.me dashboard
/// (Account &gt; Configurações &gt; Webhooks). Pagar.me's webhook security is credentials on the
/// endpoint itself, not HMAC payload signing, and Pagar.me allows leaving them unset. Leave both
/// null/empty here to match a webhook configured without authentication; see the security note on
/// <see cref="WebhookAuthenticator"/> before doing that in production.
/// </summary>
public sealed class WebhookAuthenticatorOptions
{
    public const string SectionName = "ConnectStone:Webhook";

    public string? Username { get; set; }
    public string? Password { get; set; }
}
