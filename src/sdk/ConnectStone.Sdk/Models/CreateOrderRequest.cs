namespace ConnectStone.Sdk.Models;

/// <param name="Closed">
/// Must be <see langword="false"/> for the order to appear on the POS. Orders created with
/// <see langword="true"/> never reach the card machine.
/// </param>
public sealed record CreateOrderRequest(
    CustomerRequest Customer,
    IReadOnlyList<OrderItemRequest> Items,
    bool Closed,
    PoiPaymentSettings PoiPaymentSettings);
