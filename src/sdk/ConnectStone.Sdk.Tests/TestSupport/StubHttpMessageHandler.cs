using System.Net;

namespace ConnectStone.Sdk.Tests.TestSupport;

/// <summary>
/// A minimal <see cref="HttpMessageHandler"/> test double that records every request it receives
/// and replies with a scripted sequence of responses, so client tests never hit the network.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public StubHttpMessageHandler Enqueue(HttpStatusCode statusCode, string? body = null)
    {
        _responses.Enqueue(() => new HttpResponseMessage(statusCode)
        {
            Content = body is null ? null : new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No more stubbed responses queued.");
        }

        return Task.FromResult(_responses.Dequeue().Invoke());
    }
}
