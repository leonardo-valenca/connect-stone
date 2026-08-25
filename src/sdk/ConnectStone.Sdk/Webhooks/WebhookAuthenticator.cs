using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace ConnectStone.Sdk.Webhooks;

public sealed class WebhookAuthenticator(IOptions<WebhookAuthenticatorOptions> options) : IWebhookAuthenticator
{
    private readonly WebhookAuthenticatorOptions _options = options.Value;

    /// <summary>
    /// If no username/password is configured, every request is treated as authentic, matching a
    /// Pagar.me webhook that was set up without credentials. That means anyone who finds the
    /// webhook URL can POST a fake charge.paid event and get an order marked paid without a real
    /// charge ever happening, so this is only safe for local testing or once the URL itself is
    /// kept private. Configure credentials for anything that isn't purely internal testing.
    /// </summary>
    public bool IsAuthentic(string? authorizationHeaderValue)
    {
        var credentialsConfigured = !string.IsNullOrEmpty(_options.Username) || !string.IsNullOrEmpty(_options.Password);
        if (!credentialsConfigured)
        {
            return true;
        }

        if (string.IsNullOrEmpty(authorizationHeaderValue) ||
            !authorizationHeaderValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeaderValue["Basic ".Length..]));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        var username = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];

        return FixedTimeEquals(username, _options.Username ?? string.Empty) && FixedTimeEquals(password, _options.Password ?? string.Empty);
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
