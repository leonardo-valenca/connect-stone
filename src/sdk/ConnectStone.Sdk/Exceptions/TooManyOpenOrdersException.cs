using System.Net;

namespace ConnectStone.Sdk.Exceptions;

/// <summary>
/// Thrown when order creation fails because the integration has reached Pagar.me's documented cap
/// of 30 simultaneously open orders. Detection is best-effort, see the caveat on
/// <see cref="ConnectStone.Sdk.Internal.ApiErrorResponse"/>, since Connect Stone's docs state the
/// limit exists but don't publish the exact error response shape. Close or cancel existing orders
/// before creating new ones.
/// </summary>
public sealed class TooManyOpenOrdersException(HttpStatusCode statusCode, string responseBody, string? apiMessage) : ConnectStoneApiException(statusCode, responseBody, apiMessage)
{
}
