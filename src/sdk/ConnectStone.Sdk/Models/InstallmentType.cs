using System.Text.Json.Serialization;

namespace ConnectStone.Sdk.Models;

public enum InstallmentType
{
    [JsonStringEnumMemberName("merchant")]
    Merchant,

    [JsonStringEnumMemberName("issuer")]
    Issuer,
}
