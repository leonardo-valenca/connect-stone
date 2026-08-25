namespace Domain;

public enum DemoOrderStatus
{
    Open,
    Paid,
    Canceled,
    Failed,

    /// <summary>A previously <see cref="Paid"/> order whose charge was later reversed.</summary>
    Refunded,
}
