using Application.Abstractions.ConnectStone;
using Application.Abstractions.Persistence;
using Application.Abstractions.Realtime;
using Domain;
using Domain.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Application.Orders.HandleWebhook;

public sealed class HandleWebhookCommandHandler(
    IDemoOrderRepository repository,
    IConnectStoneGateway gateway,
    IOrderStatusNotifier notifier,
    IUnitOfWork unitOfWork,
    ILogger<HandleWebhookCommandHandler> logger) : IRequestHandler<HandleWebhookCommand, Result>
{
    public async ValueTask<Result> Handle(HandleWebhookCommand command, CancellationToken cancellationToken)
    {
        var order = await repository.GetByConnectStoneOrderIdAsync(command.ConnectStoneOrderId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning("Webhook for unknown Connect Stone order {ConnectStoneOrderId} ignored.", command.ConnectStoneOrderId);
            return Result.Failure(OrderErrors.NotFound);
        }

        switch (command.Outcome)
        {
            case WebhookOutcome.Paid when order.Status == DemoOrderStatus.Open:
                order.MarkAsPaid(command.OccurredAt);
                await gateway.CloseOrderAsync(order.ConnectStoneOrderId!, ConnectStoneCloseStatus.Paid, cancellationToken);
                break;

            case WebhookOutcome.Refunded when order.Status == DemoOrderStatus.Paid:
                order.MarkAsRefunded();
                break;

            default:
                // Pagar.me delivers webhooks at-least-once, and an outcome that doesn't match the
                // order's current state is either a duplicate delivery or doesn't apply anymore.
                // Acknowledge without reprocessing rather than throwing.
                logger.LogInformation(
                    "Webhook outcome {Outcome} ignored for order {OrderId} already in status {Status}.",
                    command.Outcome, order.Id, order.Status);
                return Result.Success();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifier.NotifyOrderStatusChangedAsync(order, cancellationToken);

        return Result.Success();
    }
}
