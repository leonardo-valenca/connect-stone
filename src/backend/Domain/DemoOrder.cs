namespace Domain;

/// <summary>
/// The demo backend's own record of an order, deliberately separate from the Connect Stone SDK's
/// <c>Order</c> model. This is the local, simplified projection the dashboard displays and the
/// webhook handler updates; <see cref="ConnectStoneOrderId"/> is the join key back to the real
/// order on Pagar.me's side.
/// </summary>
public sealed class DemoOrder
{
    public Guid Id { get; private set; }
    public string? ConnectStoneOrderId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int AmountInCents { get; private set; }
    public DemoOrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    private DemoOrder()
    {
    }

    public DemoOrder(string customerName, string description, int amountInCents)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (amountInCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountInCents), amountInCents, "Amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        CustomerName = customerName;
        Description = description;
        AmountInCents = amountInCents;
        Status = DemoOrderStatus.Open;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void AttachConnectStoneOrder(string connectStoneOrderId)
    {
        ConnectStoneOrderId = connectStoneOrderId;
    }

    public void MarkAsPaid(DateTimeOffset paidAt)
    {
        EnsureOpen();
        Status = DemoOrderStatus.Paid;
        PaidAt = paidAt;
    }

    public void MarkAsCanceled()
    {
        EnsureOpen();
        Status = DemoOrderStatus.Canceled;
    }

    public void MarkAsFailed()
    {
        EnsureOpen();
        Status = DemoOrderStatus.Failed;
    }

    /// <summary>A refund only makes sense against a charge that was actually paid.</summary>
    public void MarkAsRefunded()
    {
        if (Status != DemoOrderStatus.Paid)
        {
            throw new InvalidOperationException($"Order {Id} must be Paid to be refunded, but is {Status}.");
        }

        Status = DemoOrderStatus.Refunded;
    }

    private void EnsureOpen()
    {
        if (Status != DemoOrderStatus.Open)
        {
            throw new InvalidOperationException($"Order {Id} is already {Status} and cannot transition again.");
        }
    }
}
