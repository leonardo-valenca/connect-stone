namespace ConnectStone.Sdk.Webhooks;

public sealed record WebhookAccount(string Id, string Name);

public sealed record WebhookCustomer(
    string Id,
    string Name,
    bool Delinquent,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WebhookOrder(
    string Id,
    string? Code,
    int Amount,
    bool Closed,
    string Currency,
    string Status,
    string? CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WebhookTransaction(
    string TransactionType,
    string Id,
    int Amount,
    string Status,
    bool Success,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// The charge and its related resources. Pagar.me nests all of this under the webhook's top-level
/// "data" property, and the charge's own fields (Id, Amount, Status, ...) live directly on "data"
/// rather than under a further nested "charge" object, confirmed against a real webhook delivery.
/// The transaction is named "last_transaction" in the payload, not "transaction".
/// </summary>
public sealed record WebhookChargeData(
    string Id,
    string? Code,
    int Amount,
    int? PaidAmount,
    string Status,
    string Currency,
    string? PaymentMethod,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool PendingCancellation,
    WebhookCustomer Customer,
    WebhookOrder Order,
    WebhookTransaction LastTransaction);
