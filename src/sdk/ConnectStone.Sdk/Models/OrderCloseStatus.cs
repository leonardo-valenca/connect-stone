using System.Text.Json.Serialization;

namespace ConnectStone.Sdk.Models;

/// <summary>
/// The statuses an order can be closed with. Deliberately narrower than <see cref="OrderStatus"/>,
/// "pending" is not a valid target state for closing an order.
/// </summary>
public enum OrderCloseStatus
{
    [JsonStringEnumMemberName("paid")]
    Paid,

    [JsonStringEnumMemberName("canceled")]
    Canceled,

    [JsonStringEnumMemberName("failed")]
    Failed,
}
