using Polly;

namespace ConnectStone.Sdk.Tests.TestSupport;

internal static class ClientFactory
{
    public static ConnectStoneClient Create(
        StubHttpMessageHandler handler,
        ResiliencePipeline<HttpResponseMessage>? retryPipeline = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.pagar.me/"),
        };
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("sk_test_123:")));
        httpClient.DefaultRequestHeaders.Add("ServiceRefererName", "test-partner");

        return new ConnectStoneClient(httpClient, retryPipeline ?? ResiliencePipeline<HttpResponseMessage>.Empty);
    }
}
