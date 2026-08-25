using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConnectStone.Sdk.Webhooks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConnectStoneWebhooks(
        this IServiceCollection services,
        Action<WebhookAuthenticatorOptions> configure)
    {
        // Username/password are optional (Pagar.me allows an unauthenticated webhook, see
        // WebhookAuthenticator), but having exactly one of the two set is almost always a typo
        // rather than an intentional choice, so that combination fails fast instead of silently
        // rejecting every real webhook at runtime.
        services.AddOptions<WebhookAuthenticatorOptions>()
            .Configure(configure)
            .Validate(
                o => string.IsNullOrEmpty(o.Username) == string.IsNullOrEmpty(o.Password),
                $"{nameof(WebhookAuthenticatorOptions.Username)} and {nameof(WebhookAuthenticatorOptions.Password)} must either both be set or both be left empty.")
            .ValidateOnStart();

        services.AddSingleton<IWebhookAuthenticator, WebhookAuthenticator>();
        services.AddSingleton<IWebhookEventParser, WebhookEventParser>();

        return services;
    }

    public static IServiceCollection AddConnectStoneWebhooks(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(WebhookAuthenticatorOptions.SectionName);
        return services.AddConnectStoneWebhooks(section.Bind);
    }
}
