using Domain.Common;
using Mediator;

namespace Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(string CustomerName, string Description, int AmountInCents) : IRequest<Result<OrderResponse>>;
