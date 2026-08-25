using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConnectStone.Sdk.Exceptions;
using ConnectStone.Sdk.Internal;
using ConnectStone.Sdk.Models;
using Polly;

namespace ConnectStone.Sdk;

public sealed class ConnectStoneClient : IConnectStoneClient
{
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline<HttpResponseMessage> _idempotentRetryPipeline;

    public ConnectStoneClient(HttpClient httpClient, ResiliencePipeline<HttpResponseMessage> idempotentRetryPipeline)
    {
        _httpClient = httpClient;
        _idempotentRetryPipeline = idempotentRetryPipeline;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        // Deliberately not sent through the retry pipeline: Pagar.me does not document an
        // idempotency key for order creation, so blindly retrying a POST risks creating duplicate
        // orders on the physical machine if the first attempt actually succeeded but the response
        // was lost.
        using var response = await _httpClient
            .PostAsJsonAsync("core/v5/orders/", request, ConnectStoneJson.Options, cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return await response.Content
            .ReadFromJsonAsync<Order>(ConnectStoneJson.Options, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ConnectStoneApiException(response.StatusCode, string.Empty, "Empty response body.");
    }

    public async Task<Order> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        using var response = await _idempotentRetryPipeline
            .ExecuteAsync(
                async ct => await _httpClient.GetAsync($"core/v5/orders/{Uri.EscapeDataString(orderId)}", ct).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        return await response.Content
            .ReadFromJsonAsync<Order>(ConnectStoneJson.Options, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new ConnectStoneApiException(response.StatusCode, string.Empty, "Empty response body.");
    }

    public async Task CloseOrderAsync(string orderId, OrderCloseStatus status, CancellationToken cancellationToken = default)
    {
        var body = new { status };

        using var response = await _idempotentRetryPipeline
            .ExecuteAsync(
                async ct =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Patch, $"core/v5/orders/{Uri.EscapeDataString(orderId)}/closed")
                    {
                        Content = JsonContent.Create(body, options: ConnectStoneJson.Options),
                    };
                    return await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                },
                cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelChargeAsync(string chargeId, int? amount = null, CancellationToken cancellationToken = default)
    {
        // Not retried: a second DELETE against an already-cancelled charge is not guaranteed to be
        // a safe no-op (undocumented), so a lost response shouldn't trigger an automatic resend.
        var request = new HttpRequestMessage(HttpMethod.Delete, $"core/v5/charges/{Uri.EscapeDataString(chargeId)}");
        if (amount is not null)
        {
            request.Content = JsonContent.Create(new { amount }, options: ConnectStoneJson.Options);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string? apiMessage = null;
        ApiErrorResponse? parsed = null;
        try
        {
            parsed = JsonSerializer.Deserialize<ApiErrorResponse>(body, ConnectStoneJson.Options);
            apiMessage = parsed?.Message;
        }
        catch (JsonException)
        {
            // Body wasn't the expected error envelope, fall through with the raw text preserved.
        }

        if (LooksLikeOpenOrderLimit(response.StatusCode, apiMessage, parsed?.Errors))
        {
            throw new TooManyOpenOrdersException(response.StatusCode, body, apiMessage);
        }

        throw new ConnectStoneApiException(response.StatusCode, body, apiMessage);
    }

    private static bool LooksLikeOpenOrderLimit(
        HttpStatusCode statusCode,
        string? apiMessage,
        Dictionary<string, List<string>>? errors)
    {
        if (statusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity))
        {
            return false;
        }

        var haystacks = new List<string>();
        if (apiMessage is not null)
        {
            haystacks.Add(apiMessage);
        }

        if (errors is not null)
        {
            haystacks.AddRange(errors.Values.SelectMany(v => v));
        }

        return haystacks.Any(text =>
            text.Contains("30", StringComparison.Ordinal) &&
            (text.Contains("open", StringComparison.OrdinalIgnoreCase) ||
             text.Contains("aberto", StringComparison.OrdinalIgnoreCase) ||
             text.Contains("aberta", StringComparison.OrdinalIgnoreCase)));
    }
}
