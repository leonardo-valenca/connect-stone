using Application.Abstractions.ConnectStone;
using Application.Abstractions.Persistence;
using Application.Abstractions.Realtime;
using Application.Orders;
using Application.Orders.HandleWebhook;
using Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Orders.HandleWebhook;

public class HandleWebhookCommandHandlerTests
{
    private readonly IDemoOrderRepository _repository = Substitute.For<IDemoOrderRepository>();
    private readonly IConnectStoneGateway _gateway = Substitute.For<IConnectStoneGateway>();
    private readonly IOrderStatusNotifier _notifier = Substitute.For<IOrderStatusNotifier>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly HandleWebhookCommandHandler _handler;

    public HandleWebhookCommandHandlerTests()
    {
        _handler = new HandleWebhookCommandHandler(
            _repository, _gateway, _notifier, _unitOfWork, Substitute.For<ILogger<HandleWebhookCommandHandler>>());
    }

    private static DemoOrder OpenOrder()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);
        order.AttachConnectStoneOrder("or_123");
        return order;
    }

    [Fact]
    public async Task Paid_outcome_on_open_order_marks_paid_and_closes_gateway_order()
    {
        var order = OpenOrder();
        _repository.GetByConnectStoneOrderIdAsync("or_123", Arg.Any<CancellationToken>()).Returns(order);

        var occurredAt = DateTimeOffset.UtcNow;
        var result = await _handler.Handle(new HandleWebhookCommand("or_123", WebhookOutcome.Paid, occurredAt), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DemoOrderStatus.Paid, order.Status);
        Assert.Equal(occurredAt, order.PaidAt);
        await _gateway.Received(1).CloseOrderAsync("or_123", ConnectStoneCloseStatus.Paid, Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyOrderStatusChangedAsync(order, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refunded_outcome_on_paid_order_marks_refunded_without_closing_again()
    {
        var order = OpenOrder();
        order.MarkAsPaid(DateTimeOffset.UtcNow);
        _repository.GetByConnectStoneOrderIdAsync("or_123", Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(new HandleWebhookCommand("or_123", WebhookOutcome.Refunded, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DemoOrderStatus.Refunded, order.Status);
        await _gateway.DidNotReceive().CloseOrderAsync(Arg.Any<string>(), Arg.Any<ConnectStoneCloseStatus>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_order_returns_not_found()
    {
        _repository.GetByConnectStoneOrderIdAsync("missing", Arg.Any<CancellationToken>()).Returns((DemoOrder?)null);

        var result = await _handler.Handle(new HandleWebhookCommand("missing", WebhookOutcome.Paid, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Duplicate_paid_webhook_on_already_paid_order_is_acknowledged_without_reprocessing()
    {
        var order = OpenOrder();
        order.MarkAsPaid(DateTimeOffset.UtcNow);
        _repository.GetByConnectStoneOrderIdAsync("or_123", Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(new HandleWebhookCommand("or_123", WebhookOutcome.Paid, DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _gateway.DidNotReceive().CloseOrderAsync(Arg.Any<string>(), Arg.Any<ConnectStoneCloseStatus>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
