using Application.Abstractions.Persistence;
using Application.Orders.GetOrders;
using Domain;
using NSubstitute;

namespace Application.Tests.Orders.GetOrders;

public class GetOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_orders_newest_first()
    {
        var older = new DemoOrder("Jane Doe", "Coffee", 1000);
        await Task.Delay(5);
        var newer = new DemoOrder("John Smith", "Tea", 2000);

        var repository = Substitute.For<IDemoOrderRepository>();
        repository.ListAsync(Arg.Any<CancellationToken>()).Returns<IReadOnlyList<DemoOrder>>([older, newer]);

        var handler = new GetOrdersQueryHandler(repository);
        var result = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(newer.Id, result.Value[0].Id);
        Assert.Equal(older.Id, result.Value[1].Id);
    }
}
