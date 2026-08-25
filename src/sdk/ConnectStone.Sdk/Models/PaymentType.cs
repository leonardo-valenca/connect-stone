using System.Text.Json.Serialization;

namespace ConnectStone.Sdk.Models;

public enum PaymentType
{
    [JsonStringEnumMemberName("debit")]
    Debit,

    [JsonStringEnumMemberName("credit")]
    Credit,

    [JsonStringEnumMemberName("voucher")]
    Voucher,

    [JsonStringEnumMemberName("pix")]
    Pix,
}
