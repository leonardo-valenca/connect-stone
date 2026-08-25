using Application.Abstractions.Persistence;
using Application.Orders.HandleWebhook;
using Domain.Common;
using Mediator;

namespace Application.Orders.SimulatePayment;

public sealed class SimulatePaymentCommandHandler(IDemoOrderRepository repository, IMediator mediator)
    : IRequestHandler<SimulatePaymentCommand, Result>
{
    public async ValueTask<Result> Handle(SimulatePaymentCommand command, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound);
        }

        if (order.ConnectStoneOrderId is null)
        {
            return Result.Failure(OrderErrors.MissingConnectStoneOrderId);
        }

        return await mediator.Send(
            new HandleWebhookCommand(order.ConnectStoneOrderId, WebhookOutcome.Paid, DateTimeOffset.UtcNow),
            cancellationToken);
    }
}
