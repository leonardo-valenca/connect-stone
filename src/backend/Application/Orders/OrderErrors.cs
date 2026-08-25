using Domain.Common;

namespace Application.Orders;

public static class OrderErrors
{
    public static readonly Error TooManyOpenOrders = new(
        "Orders.TooManyOpen",
        "This integration already has 30 open orders. Close or cancel existing orders before creating new ones.");

    public static readonly Error NotFound = new("Orders.NotFound", "Order not found.");

    public static readonly Error MissingConnectStoneOrderId = new(
        "Orders.MissingConnectStoneOrderId",
        "This order was never linked to a Connect Stone order.");
}
