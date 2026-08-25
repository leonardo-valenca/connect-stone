using Application.Abstractions.Realtime;
using Application.Orders;
using Domain;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Realtime;

public sealed class SignalROrderStatusNotifier(IHubContext<OrderStatusHub> hubContext) : IOrderStatusNotifier
{
    public Task NotifyOrderStatusChangedAsync(DemoOrder order, CancellationToken cancellationToken) =>
        hubContext.Clients.All.SendAsync("OrderStatusChanged", OrderResponse.FromDomain(order), cancellationToken);
}
