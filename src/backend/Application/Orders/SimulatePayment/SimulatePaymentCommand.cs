using Domain.Common;
using Mediator;

namespace Application.Orders.SimulatePayment;

/// <summary>
/// Demo-only: synthesizes the same outcome a real <c>charge.paid</c> webhook would produce, driven
/// through the exact same <see cref="HandleWebhook.HandleWebhookCommand"/> path, so simulate mode
/// exercises the real webhook-handling logic instead of a shortcut around it.
/// </summary>
public sealed record SimulatePaymentCommand(Guid OrderId) : IRequest<Result>;
