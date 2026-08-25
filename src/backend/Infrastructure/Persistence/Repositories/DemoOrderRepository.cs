using Application.Abstractions.Persistence;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class DemoOrderRepository(AppDbContext dbContext) : IDemoOrderRepository
{
    public async Task AddAsync(DemoOrder order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task<DemoOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<DemoOrder?> GetByConnectStoneOrderIdAsync(string connectStoneOrderId, CancellationToken cancellationToken) =>
        dbContext.Orders.SingleOrDefaultAsync(o => o.ConnectStoneOrderId == connectStoneOrderId, cancellationToken);

    public async Task<IReadOnlyList<DemoOrder>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Orders.AsNoTracking().ToListAsync(cancellationToken);

    public Task<int> CountOpenAsync(CancellationToken cancellationToken) =>
        dbContext.Orders.CountAsync(o => o.Status == DemoOrderStatus.Open, cancellationToken);
}
