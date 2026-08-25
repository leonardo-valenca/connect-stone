using Domain;

namespace Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    string? ConnectStoneOrderId,
    string CustomerName,
    string Description,
    int AmountInCents,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt)
{
    public static OrderResponse FromDomain(DemoOrder order) => new(
        order.Id,
        order.ConnectStoneOrderId,
        order.CustomerName,
        order.Description,
        order.AmountInCents,
        order.Status.ToString(),
        order.CreatedAt,
        order.PaidAt);
}
