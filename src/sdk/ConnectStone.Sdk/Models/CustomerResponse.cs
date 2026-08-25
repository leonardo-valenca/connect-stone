namespace ConnectStone.Sdk.Models;

public sealed record CustomerResponse(
    string Id,
    string Name,
    string? Email,
    bool Delinquent,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
