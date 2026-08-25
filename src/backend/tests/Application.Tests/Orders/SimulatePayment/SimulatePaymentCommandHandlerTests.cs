using Application.Abstractions.Persistence;
using Application.Orders;
using Application.Orders.HandleWebhook;
using Application.Orders.SimulatePayment;
using Domain;
using Domain.Common;
using Mediator;
using NSubstitute;

namespace Application.Tests.Orders.SimulatePayment;

public class SimulatePaymentCommandHandlerTests
{
    private readonly IDemoOrderRepository _repository = Substitute.For<IDemoOrderRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly SimulatePaymentCommandHandler _handler;

    public SimulatePaymentCommandHandlerTests()
    {
        _handler = new SimulatePaymentCommandHandler(_repository, _mediator);
    }

    [Fact]
    public async Task Order_not_found_returns_not_found_error()
    {
        var orderId = Guid.NewGuid();
        _repository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((DemoOrder?)null);

        var result = await _handler.Handle(new SimulatePaymentCommand(orderId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Order_without_connect_stone_id_returns_error()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(new SimulatePaymentCommand(order.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.MissingConnectStoneOrderId, result.Error);
    }

    [Fact]
    public async Task Valid_order_dispatches_handle_webhook_command_with_paid_outcome()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);
        order.AttachConnectStoneOrder("or_123");
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _mediator.Send(Arg.Any<HandleWebhookCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _handler.Handle(new SimulatePaymentCommand(order.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _mediator.Received(1).Send(
            Arg.Is<HandleWebhookCommand>(c => c.ConnectStoneOrderId == "or_123" && c.Outcome == WebhookOutcome.Paid),
            Arg.Any<CancellationToken>());
    }
}
