using Application.Abstractions.ConnectStone;
using Application.Abstractions.Persistence;
using Application.Orders;
using Application.Orders.CreateOrder;
using Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Orders.CreateOrder;

public class CreateOrderCommandHandlerTests
{
    private readonly IDemoOrderRepository _repository = Substitute.For<IDemoOrderRepository>();
    private readonly IConnectStoneGateway _gateway = Substitute.For<IConnectStoneGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _handler = new CreateOrderCommandHandler(_repository, _gateway, _unitOfWork, Substitute.For<ILogger<CreateOrderCommandHandler>>());
    }

    [Fact]
    public async Task Handle_creates_order_and_attaches_connect_stone_id()
    {
        _gateway.CreateOrderAsync(Arg.Any<DemoOrder>(), Arg.Any<CancellationToken>()).Returns("or_123");

        var result = await _handler.Handle(new CreateOrderCommand("Jane Doe", "Coffee", 1500), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("or_123", result.Value.ConnectStoneOrderId);
        Assert.Equal("Open", result.Value.Status);
        await _repository.Received(1).AddAsync(Arg.Is<DemoOrder>(o => o.ConnectStoneOrderId == "or_123"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_failure_when_open_order_limit_reached()
    {
        _gateway.CreateOrderAsync(Arg.Any<DemoOrder>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new ConnectStoneOrderLimitExceededException());

        var result = await _handler.Handle(new CreateOrderCommand("Jane Doe", "Coffee", 1500), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.TooManyOpenOrders, result.Error);
        await _repository.DidNotReceive().AddAsync(Arg.Any<DemoOrder>(), Arg.Any<CancellationToken>());
    }
}
