using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Realtime;

/// <summary>Server-to-client push only, the dashboard never calls back into this hub, it just listens.</summary>
public sealed class OrderStatusHub : Hub;
