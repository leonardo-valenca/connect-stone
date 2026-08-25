using ConnectStone.Sdk.Internal;

namespace ConnectStone.Sdk.Webhooks;

public sealed class WebhookEventParser : IWebhookEventParser
{
    public ConnectStoneWebhookEvent? Parse(string rawJsonBody)
    {
        var envelope = System.Text.Json.JsonSerializer.Deserialize<WebhookEnvelope>(rawJsonBody, ConnectStoneJson.Options);
        if (envelope is null)
        {
            return null;
        }

        return envelope.Type switch
        {
            "charge.paid" => new ChargePaidEvent(envelope.Id, envelope.Account, envelope.CreatedAt, envelope.Data),
            "charge.refunded" => new ChargeRefundedEvent(envelope.Id, envelope.Account, envelope.CreatedAt, envelope.Data),
            _ => null,
        };
    }

    private sealed record WebhookEnvelope(
        string Id,
        WebhookAccount Account,
        string Type,
        DateTimeOffset CreatedAt,
        WebhookChargeData Data);
}
