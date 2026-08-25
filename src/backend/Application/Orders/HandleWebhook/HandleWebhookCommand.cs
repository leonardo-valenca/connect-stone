using Domain.Common;
using Mediator;

namespace Application.Orders.HandleWebhook;

public enum WebhookOutcome
{
    Paid,
    Refunded,
}

/// <param name="ConnectStoneOrderId">Used to find the matching local <c>DemoOrder</c>.</param>
/// <param name="OccurredAt">
/// The event's own timestamp (e.g. <c>charge.paid_at</c>), recorded as the order's PaidAt instead
/// of "now" so the dashboard reflects when the machine actually processed the payment.
/// </param>
public sealed record HandleWebhookCommand(string ConnectStoneOrderId, WebhookOutcome Outcome, DateTimeOffset OccurredAt)
    : IRequest<Result>;
