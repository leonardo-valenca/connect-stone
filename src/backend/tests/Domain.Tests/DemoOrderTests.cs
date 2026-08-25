namespace Domain.Tests;

public class DemoOrderTests
{
    [Fact]
    public void Constructor_sets_open_status_and_timestamps()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);

        Assert.Equal(DemoOrderStatus.Open, order.Status);
        Assert.Null(order.ConnectStoneOrderId);
        Assert.Null(order.PaidAt);
        Assert.NotEqual(Guid.Empty, order.Id);
    }

    [Theory]
    [InlineData("", "Coffee", 1500)]
    [InlineData(" ", "Coffee", 1500)]
    public void Constructor_rejects_blank_customer_name(string customerName, string description, int amount)
    {
        Assert.Throws<ArgumentException>(() => new DemoOrder(customerName, description, amount));
    }

    [Theory]
    [InlineData("Jane Doe", "", 1500)]
    [InlineData("Jane Doe", " ", 1500)]
    public void Constructor_rejects_blank_description(string customerName, string description, int amount)
    {
        Assert.Throws<ArgumentException>(() => new DemoOrder(customerName, description, amount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_rejects_non_positive_amount(int amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DemoOrder("Jane Doe", "Coffee", amount));
    }

    [Fact]
    public void MarkAsPaid_from_open_succeeds()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);
        var paidAt = DateTimeOffset.UtcNow;

        order.MarkAsPaid(paidAt);

        Assert.Equal(DemoOrderStatus.Paid, order.Status);
        Assert.Equal(paidAt, order.PaidAt);
    }

    [Fact]
    public void MarkAsPaid_from_non_open_throws()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);
        order.MarkAsPaid(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => order.MarkAsPaid(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkAsRefunded_requires_paid_status()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);

        Assert.Throws<InvalidOperationException>(order.MarkAsRefunded);
    }

    [Fact]
    public void MarkAsRefunded_from_paid_succeeds()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);
        order.MarkAsPaid(DateTimeOffset.UtcNow);

        order.MarkAsRefunded();

        Assert.Equal(DemoOrderStatus.Refunded, order.Status);
    }

    [Fact]
    public void MarkAsCanceled_and_MarkAsFailed_require_open_status()
    {
        var canceledOrder = new DemoOrder("Jane Doe", "Coffee", 1500);
        canceledOrder.MarkAsCanceled();
        Assert.Equal(DemoOrderStatus.Canceled, canceledOrder.Status);

        var failedOrder = new DemoOrder("Jane Doe", "Coffee", 1500);
        failedOrder.MarkAsFailed();
        Assert.Equal(DemoOrderStatus.Failed, failedOrder.Status);

        Assert.Throws<InvalidOperationException>(canceledOrder.MarkAsCanceled);
    }

    [Fact]
    public void AttachConnectStoneOrder_sets_the_id()
    {
        var order = new DemoOrder("Jane Doe", "Coffee", 1500);

        order.AttachConnectStoneOrder("or_123");

        Assert.Equal("or_123", order.ConnectStoneOrderId);
    }
}
