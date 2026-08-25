using System.Net;
using System.Text.Json;
using ConnectStone.Sdk.Exceptions;
using ConnectStone.Sdk.Models;
using ConnectStone.Sdk.Tests.TestSupport;
using Polly;
using Polly.Retry;

namespace ConnectStone.Sdk.Tests;

public class ConnectStoneClientTests
{
    private const string SampleOrderJson = """
        {
          "id": "or_123",
          "code": "code123",
          "amount": 1000,
          "currency": "BRL",
          "status": "pending",
          "closed": false,
          "created_at": "2026-08-20T10:00:00Z",
          "updated_at": "2026-08-20T10:00:00Z",
          "items": [
            { "id": "oi_1", "type": null, "description": "Coffee", "amount": 1000, "quantity": 1, "status": null, "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:00:00Z" }
          ],
          "customer": { "id": "cus_1", "name": "Jane Doe", "email": "jane@example.com", "delinquent": false, "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:00:00Z" },
          "poi_payment_settings": null
        }
        """;

    private static CreateOrderRequest SampleCreateRequest() => new(
        Customer: new CustomerRequest("Jane Doe", "jane@example.com"),
        Items: [new OrderItemRequest(1000, "Coffee", 1)],
        Closed: false,
        PoiPaymentSettings: new PoiPaymentSettings(
            Type: PaymentType.Credit,
            Installments: 1,
            InstallmentType: InstallmentType.Merchant,
            Visible: true,
            DisplayName: "Coffee shop",
            PrintOrderReceipt: true));

    [Fact]
    public async Task CreateOrderAsync_sends_expected_request_and_parses_response()
    {
        var handler = new StubHttpMessageHandler().Enqueue(HttpStatusCode.OK, SampleOrderJson);
        var client = ClientFactory.Create(handler);

        var order = await client.CreateOrderAsync(SampleCreateRequest());

        Assert.Equal("or_123", order.Id);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.Items);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.pagar.me/core/v5/orders/", request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);

        var body = await request.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Jane Doe", json.RootElement.GetProperty("customer").GetProperty("name").GetString());
        Assert.Equal("credit", json.RootElement.GetProperty("poi_payment_settings").GetProperty("type").GetString());
    }

    [Fact]
    public async Task CreateOrderAsync_throws_ConnectStoneApiException_on_generic_error()
    {
        var handler = new StubHttpMessageHandler().Enqueue(
            HttpStatusCode.BadRequest, """{ "message": "invalid customer email" }""");
        var client = ClientFactory.Create(handler);

        var exception = await Assert.ThrowsAsync<ConnectStoneApiException>(() => client.CreateOrderAsync(SampleCreateRequest()));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("invalid customer email", exception.ApiMessage);
        Assert.IsNotType<TooManyOpenOrdersException>(exception);
    }

    [Fact]
    public async Task CreateOrderAsync_throws_TooManyOpenOrdersException_when_error_mentions_open_order_cap()
    {
        var handler = new StubHttpMessageHandler().Enqueue(
            HttpStatusCode.UnprocessableEntity, """{ "message": "you already have 30 open orders" }""");
        var client = ClientFactory.Create(handler);

        await Assert.ThrowsAsync<TooManyOpenOrdersException>(() => client.CreateOrderAsync(SampleCreateRequest()));
    }

    [Fact]
    public async Task CreateOrderAsync_is_not_retried_on_server_error()
    {
        var handler = new StubHttpMessageHandler().Enqueue(HttpStatusCode.InternalServerError, "{}");
        var client = ClientFactory.Create(handler, BuildTestRetryPipeline());

        await Assert.ThrowsAsync<ConnectStoneApiException>(() => client.CreateOrderAsync(SampleCreateRequest()));

        Assert.Single(handler.Requests); // no retry attempts for POST
    }

    [Fact]
    public async Task GetOrderAsync_retries_transient_server_errors_then_succeeds()
    {
        var handler = new StubHttpMessageHandler()
            .Enqueue(HttpStatusCode.ServiceUnavailable, "{}")
            .Enqueue(HttpStatusCode.OK, SampleOrderJson);
        var client = ClientFactory.Create(handler, BuildTestRetryPipeline());

        var order = await client.GetOrderAsync("or_123");

        Assert.Equal("or_123", order.Id);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
        Assert.Equal("https://api.pagar.me/core/v5/orders/or_123", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task CloseOrderAsync_sends_patch_with_status_body()
    {
        var handler = new StubHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{}");
        var client = ClientFactory.Create(handler);

        await client.CloseOrderAsync("or_123", OrderCloseStatus.Paid);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal("https://api.pagar.me/core/v5/orders/or_123/closed", request.RequestUri!.ToString());

        var body = await request.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal("paid", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelChargeAsync_sends_delete_without_body_when_amount_omitted()
    {
        var handler = new StubHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{}");
        var client = ClientFactory.Create(handler);

        await client.CancelChargeAsync("ch_123");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("https://api.pagar.me/core/v5/charges/ch_123", request.RequestUri!.ToString());
        Assert.Null(request.Content);
    }

    [Fact]
    public async Task CancelChargeAsync_sends_partial_amount_when_specified()
    {
        var handler = new StubHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{}");
        var client = ClientFactory.Create(handler);

        await client.CancelChargeAsync("ch_123", amount: 500);

        var request = Assert.Single(handler.Requests);
        var body = await request.Content!.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("amount").GetInt32());
    }

    private static ResiliencePipeline<HttpResponseMessage> BuildTestRetryPipeline() =>
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500),
            })
            .Build();
}
