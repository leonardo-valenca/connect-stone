using ConnectStone.Sdk.Models;

namespace ConnectStone.Sdk;

public interface IConnectStoneClient
{
    /// <summary>
    /// Creates an order. With <c>Closed: false</c>, it becomes visible on the linked card machine(s).
    /// </summary>
    /// <exception cref="Exceptions.TooManyOpenOrdersException">
    /// The integration already has 30 open orders (Pagar.me's documented cap).
    /// </exception>
    Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<Order> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an order (removes it from the POS). Call this once a webhook confirms the payment
    /// outcome, leaving paid or failed orders open can cause the POS to misbehave.
    /// </summary>
    Task CloseOrderAsync(string orderId, OrderCloseStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels (voids/refunds) a charge that has already been processed. This is a distinct
    /// operation from <see cref="CloseOrderAsync"/>: it acts on the charge resource, not the order,
    /// and triggers a <c>charge.refunded</c> webhook. Omit <paramref name="amount"/> to cancel the
    /// full charge, or supply it (in cents) for a partial cancellation.
    /// </summary>
    Task CancelChargeAsync(string chargeId, int? amount = null, CancellationToken cancellationToken = default);
}
