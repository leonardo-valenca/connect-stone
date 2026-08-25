using Domain;

namespace Application.Abstractions.Realtime;

/// <summary>Pushes order status changes to connected dashboard clients (implemented via SignalR in Infrastructure).</summary>
public interface IOrderStatusNotifier
{
    Task NotifyOrderStatusChangedAsync(DemoOrder order, CancellationToken cancellationToken);
}
