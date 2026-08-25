using System.Text.Json.Serialization;

namespace ConnectStone.Sdk.Models;

public enum OrderStatus
{
    [JsonStringEnumMemberName("pending")]
    Pending,

    [JsonStringEnumMemberName("paid")]
    Paid,

    [JsonStringEnumMemberName("canceled")]
    Canceled,

    [JsonStringEnumMemberName("failed")]
    Failed,
}
