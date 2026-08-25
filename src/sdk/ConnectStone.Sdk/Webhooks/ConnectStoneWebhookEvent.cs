namespace ConnectStone.Sdk.Webhooks;

/// <summary>
/// Base type for the webhook events this SDK understands. Pattern-match on the concrete type
/// (<see cref="ChargePaidEvent"/>, <see cref="ChargeRefundedEvent"/>) to access event-specific data.
/// </summary>
public abstract record ConnectStoneWebhookEvent(
    string Id,
    WebhookAccount Account,
    DateTimeOffset CreatedAt,
    WebhookChargeData Data);

/// <summary>Fired when a charge is successfully paid on the card machine.</summary>
public sealed record ChargePaidEvent(string Id, WebhookAccount Account, DateTimeOffset CreatedAt, WebhookChargeData Data)
    : ConnectStoneWebhookEvent(Id, Account, CreatedAt, Data);

/// <summary>Fired when a previously paid charge is reversed/cancelled.</summary>
public sealed record ChargeRefundedEvent(string Id, WebhookAccount Account, DateTimeOffset CreatedAt, WebhookChargeData Data)
    : ConnectStoneWebhookEvent(Id, Account, CreatedAt, Data);
