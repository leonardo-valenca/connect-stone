using Application.Abstractions.ConnectStone;
using Application.Abstractions.Persistence;
using Domain;
using Domain.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler(
    IDemoOrderRepository repository,
    IConnectStoneGateway gateway,
    IUnitOfWork unitOfWork,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, Result<OrderResponse>>
{
    public async ValueTask<Result<OrderResponse>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var order = new DemoOrder(command.CustomerName, command.Description, command.AmountInCents);

        string connectStoneOrderId;
        try
        {
            connectStoneOrderId = await gateway.CreateOrderAsync(order, cancellationToken);
        }
        catch (ConnectStoneOrderLimitExceededException)
        {
            logger.LogWarning("Order creation rejected: open-order limit reached.");
            return Result.Failure<OrderResponse>(OrderErrors.TooManyOpenOrders);
        }

        order.AttachConnectStoneOrder(connectStoneOrderId);

        await repository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created order {OrderId} (Connect Stone id {ConnectStoneOrderId}).", order.Id, connectStoneOrderId);

        return OrderResponse.FromDomain(order);
    }
}
