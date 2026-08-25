namespace ConnectStone.Sdk.Models;

/// <param name="Name">Required, max 64 characters.</param>
/// <param name="Email">Optional, max 64 characters.</param>
public sealed record CustomerRequest(string Name, string? Email = null);
