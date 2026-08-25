using Application.Abstractions.Persistence;
using Domain.Common;
using Mediator;

namespace Application.Orders.GetOrders;

public sealed class GetOrdersQueryHandler(IDemoOrderRepository repository)
    : IRequestHandler<GetOrdersQuery, Result<IReadOnlyList<OrderResponse>>>
{
    public async ValueTask<Result<IReadOnlyList<OrderResponse>>> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = await repository.ListAsync(cancellationToken);

        IReadOnlyList<OrderResponse> response = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(OrderResponse.FromDomain)
            .ToList();

        return Result.Success(response);
    }
}
