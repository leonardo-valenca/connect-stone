using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Orders;

namespace Api.IntegrationTests;

public class OrderFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public OrderFlowTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Alive_and_ready_health_checks_respond()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/alive")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/ready")).StatusCode);
    }

    [Fact]
    public async Task CreateOrder_then_GetOrders_roundtrips()
    {
        var response = await _client.PostAsJsonAsync("/orders", new { customerName = "Jane Doe", description = "Coffee", amountInCents = 1500 });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(created);
        Assert.Equal("Open", created!.Status);
        Assert.StartsWith("fake_", created.ConnectStoneOrderId);

        var listResponse = await _client.GetAsync("/orders");
        var orders = await listResponse.Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.Contains(orders!, o => o.Id == created.Id);
    }

    [Fact]
    public async Task CreateOrder_with_invalid_body_returns_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/orders", new { customerName = "", description = "", amountInCents = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_without_credentials_is_rejected()
    {
        var response = await _client.PostAsync("/webhooks/pagarme", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_charge_paid_closes_order_and_notifies()
    {
        var createResponse = await _client.PostAsJsonAsync("/orders", new { customerName = "Jane Doe", description = "Coffee", amountInCents = 1500 });
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var payload = $$"""
            {
              "id": "hook_1", "account": { "id": "acc_1", "name": "Store" }, "type": "charge.paid", "created_at": "2026-08-20T10:05:00Z",
              "data": {
                "id": "ch_1", "code": "ch_1", "amount": 1500, "paid_amount": 1500, "status": "paid", "currency": "BRL", "payment_method": "credit_card", "paid_at": "2026-08-20T10:05:00Z", "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:05:00Z", "pending_cancellation": false,
                "customer": { "id": "cus_1", "name": "Jane Doe", "delinquent": false, "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:00:00Z" },
                "order": { "id": "{{created!.ConnectStoneOrderId}}", "code": "or_1", "amount": 1500, "closed": false, "currency": "BRL", "status": "pending", "customer_id": "cus_1", "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:05:00Z" },
                "last_transaction": { "transaction_type": "credit_card", "id": "tx_1", "amount": 1500, "status": "captured", "success": true, "created_at": "2026-08-20T10:05:00Z", "updated_at": "2026-08-20T10:05:00Z" }
              }
            }
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/pagarme")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("hookuser:hookpass")));

        var webhookResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var orders = await (await _client.GetAsync("/orders")).Content.ReadFromJsonAsync<List<OrderResponse>>();
        var updated = orders!.Single(o => o.Id == created.Id);
        Assert.Equal("Paid", updated.Status);
        Assert.Contains(_factory.Gateway.ClosedOrders, c => c.ConnectStoneOrderId == created.ConnectStoneOrderId);
    }

    [Fact]
    public async Task SimulatePayment_marks_order_paid()
    {
        var createResponse = await _client.PostAsJsonAsync("/orders", new { customerName = "Jane Doe", description = "Tea", amountInCents = 1000 });
        var created = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var simulateResponse = await _client.PostAsync($"/demo/simulate-payment/{created!.Id}", content: null);
        Assert.Equal(HttpStatusCode.OK, simulateResponse.StatusCode);

        var orders = await (await _client.GetAsync("/orders")).Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.Equal("Paid", orders!.Single(o => o.Id == created.Id).Status);
    }

    [Fact]
    public async Task SimulatePayment_for_unknown_order_returns_not_found()
    {
        var response = await _client.PostAsync($"/demo/simulate-payment/{Guid.NewGuid()}", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
