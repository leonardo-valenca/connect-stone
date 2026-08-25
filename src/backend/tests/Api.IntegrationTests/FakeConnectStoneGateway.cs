using Application.Abstractions.ConnectStone;
using Domain;

namespace Api.IntegrationTests;

/// <summary>Stands in for the real Pagar.me API so integration tests never touch the network.</summary>
public sealed class FakeConnectStoneGateway : IConnectStoneGateway
{
    public List<(string ConnectStoneOrderId, ConnectStoneCloseStatus Status)> ClosedOrders { get; } = [];

    public Task<string> CreateOrderAsync(DemoOrder order, CancellationToken cancellationToken) =>
        Task.FromResult($"fake_{order.Id:N}");

    public Task CloseOrderAsync(string connectStoneOrderId, ConnectStoneCloseStatus status, CancellationToken cancellationToken)
    {
        ClosedOrders.Add((connectStoneOrderId, status));
        return Task.CompletedTask;
    }
}
