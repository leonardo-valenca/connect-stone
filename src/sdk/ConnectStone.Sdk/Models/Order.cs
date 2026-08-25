namespace ConnectStone.Sdk.Models;

public sealed record Order(
    string Id,
    string Code,
    int Amount,
    string Currency,
    OrderStatus Status,
    bool Closed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items,
    CustomerResponse Customer,
    PoiPaymentSettings? PoiPaymentSettings);
