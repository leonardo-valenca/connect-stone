using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConnectStone.Sdk.Internal;

internal static class ConnectStoneJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
