using ConnectStone.Sdk.Webhooks;

namespace ConnectStone.Sdk.Tests;

public class WebhookEventParserTests
{
    // Structure verified against a real webhook delivery: the charge's own fields live directly
    // on "data" (not a nested "charge"), and the transaction is "last_transaction".
    private static string SamplePayload(string type) => $$"""
        {
          "id": "hook_1",
          "account": { "id": "acc_1", "name": "My Store" },
          "type": "{{type}}",
          "created_at": "2026-08-20T10:05:00Z",
          "data": {
            "id": "ch_1", "code": "ch_code", "amount": 1000, "paid_amount": 1000, "status": "paid",
            "currency": "BRL", "payment_method": "credit_card", "paid_at": "2026-08-20T10:05:00Z",
            "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:05:00Z", "pending_cancellation": false,
            "customer": { "id": "cus_1", "name": "Jane Doe", "delinquent": false, "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:00:00Z" },
            "order": {
              "id": "or_1", "code": "or_code", "amount": 1000, "closed": false, "currency": "BRL", "status": "pending",
              "customer_id": "cus_1", "created_at": "2026-08-20T10:00:00Z", "updated_at": "2026-08-20T10:05:00Z"
            },
            "last_transaction": {
              "transaction_type": "credit_card", "id": "tx_1", "amount": 1000, "status": "captured", "success": true,
              "created_at": "2026-08-20T10:05:00Z", "updated_at": "2026-08-20T10:05:00Z"
            }
          }
        }
        """;

    private readonly WebhookEventParser _parser = new();

    [Fact]
    public void Parse_maps_charge_paid_event()
    {
        var result = _parser.Parse(SamplePayload("charge.paid"));

        var chargePaid = Assert.IsType<ChargePaidEvent>(result);
        Assert.Equal("hook_1", chargePaid.Id);
        Assert.Equal("ch_1", chargePaid.Data.Id);
        Assert.Equal("or_1", chargePaid.Data.Order.Id);
        Assert.True(chargePaid.Data.LastTransaction.Success);
    }

    [Fact]
    public void Parse_maps_charge_refunded_event()
    {
        var result = _parser.Parse(SamplePayload("charge.refunded"));

        Assert.IsType<ChargeRefundedEvent>(result);
    }

    [Fact]
    public void Parse_returns_null_for_unrecognized_event_type()
    {
        var result = _parser.Parse(SamplePayload("order.updated"));

        Assert.Null(result);
    }
}
