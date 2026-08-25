using Domain;

namespace Application.Abstractions.ConnectStone;

/// <summary>
/// The demo's own vocabulary for talking to Connect Stone. Application depends on this, not on
/// ConnectStone.Sdk directly, so it never needs to know Pagar.me's request/response shapes.
/// Infrastructure implements it by adapting to the SDK.
/// </summary>
public interface IConnectStoneGateway
{
    /// <returns>The Connect Stone order id to store on <see cref="DemoOrder.ConnectStoneOrderId"/>.</returns>
    Task<string> CreateOrderAsync(DemoOrder order, CancellationToken cancellationToken);

    Task CloseOrderAsync(string connectStoneOrderId, ConnectStoneCloseStatus status, CancellationToken cancellationToken);
}

public enum ConnectStoneCloseStatus
{
    Paid,
    Canceled,
    Failed,
}

/// <summary>
/// Thrown by <see cref="IConnectStoneGateway.CreateOrderAsync"/> when Pagar.me's 30-open-order cap
/// is hit. A translation of ConnectStone.Sdk's exception into Application's own vocabulary, so
/// Application never needs a reference to the SDK.
/// </summary>
public sealed class ConnectStoneOrderLimitExceededException()
    : Exception("The Connect Stone integration has reached its 30 open-order limit.");
