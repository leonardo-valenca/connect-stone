using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConnectStone.Sdk.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddConnectStoneClient_omits_ServiceRefererName_header_when_not_set()
    {
        var services = new ServiceCollection();
        services.AddConnectStoneClient(o => o.SecretKey = "sk_test_123");
        using var provider = services.BuildServiceProvider();

        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IConnectStoneClient));

        Assert.False(httpClient.DefaultRequestHeaders.Contains("ServiceRefererName"));
        Assert.Equal("Basic", httpClient.DefaultRequestHeaders.Authorization!.Scheme);
    }

    [Fact]
    public void AddConnectStoneClient_sends_ServiceRefererName_header_when_set()
    {
        var services = new ServiceCollection();
        services.AddConnectStoneClient(o =>
        {
            o.SecretKey = "sk_test_123";
            o.ServiceRefererName = "partner-123";
        });
        using var provider = services.BuildServiceProvider();

        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IConnectStoneClient));

        Assert.Equal("partner-123", httpClient.DefaultRequestHeaders.GetValues("ServiceRefererName").Single());
    }

    [Fact]
    public void AddConnectStoneClient_fails_startup_validation_without_a_secret_key()
    {
        var services = new ServiceCollection();
        services.AddConnectStoneClient(_ => { });
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ConnectStoneClientOptions>>().Value);
    }
}
