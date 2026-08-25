using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace ConnectStone.Sdk;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IConnectStoneClient"/> and its <see cref="HttpClient"/>, configured with
    /// Connect Stone's Basic-auth scheme and a retry pipeline applied only to the idempotent
    /// GET/PATCH operations (see <see cref="ConnectStoneClient"/> for why POST/DELETE are excluded).
    /// </summary>
    public static IServiceCollection AddConnectStoneClient(
        this IServiceCollection services,
        Action<ConnectStoneClientOptions> configure)
    {
        services.AddOptions<ConnectStoneClientOptions>()
            .Configure(configure)
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.SecretKey),
                $"{nameof(ConnectStoneClientOptions.SecretKey)} is required.")
            .ValidateOnStart();

        services.AddHttpClient<IConnectStoneClient, ConnectStoneClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ConnectStoneClientOptions>>().Value;

            httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.SecretKey}:")));

            if (!string.IsNullOrEmpty(options.ServiceRefererName))
            {
                httpClient.DefaultRequestHeaders.Add("ServiceRefererName", options.ServiceRefererName);
            }
        });

        services.AddSingleton(BuildIdempotentRetryPipeline());

        return services;
    }

    /// <summary>
    /// Registers <see cref="IConnectStoneClient"/> bound to configuration section
    /// <see cref="ConnectStoneClientOptions.SectionName"/>.
    /// </summary>
    public static IServiceCollection AddConnectStoneClient(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ConnectStoneClientOptions.SectionName);
        return services.AddConnectStoneClient(section.Bind);
    }

    private static ResiliencePipeline<HttpResponseMessage> BuildIdempotentRetryPipeline() =>
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromMilliseconds(200),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response =>
                        (int)response.StatusCode >= 500 || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
            })
            .Build();
}
