namespace ConnectStone.Sdk.Models;

/// <param name="Amount">Unit price in cents.</param>
public sealed record OrderItemRequest(int Amount, string Description, int Quantity, string? Code = null);
