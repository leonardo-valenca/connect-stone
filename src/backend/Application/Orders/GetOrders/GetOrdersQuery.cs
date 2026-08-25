using Domain.Common;
using Mediator;

namespace Application.Orders.GetOrders;

public sealed record GetOrdersQuery : IRequest<Result<IReadOnlyList<OrderResponse>>>;
