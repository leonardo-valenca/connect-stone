namespace ConnectStone.Sdk.Models;

public sealed record OrderItemResponse(
    string Id,
    string? Type,
    string Description,
    int Amount,
    int Quantity,
    string? Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
