using Domain;

namespace Application.Abstractions.Persistence;

public interface IDemoOrderRepository
{
    Task AddAsync(DemoOrder order, CancellationToken cancellationToken);

    Task<DemoOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DemoOrder?> GetByConnectStoneOrderIdAsync(string connectStoneOrderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DemoOrder>> ListAsync(CancellationToken cancellationToken);

    Task<int> CountOpenAsync(CancellationToken cancellationToken);
}
