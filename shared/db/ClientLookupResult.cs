namespace GearFlow.Lambda.Shared.Db;

public sealed class ClientLookupResult
{
    public required string ClientId { get; init; }
    public required string Cpf { get; init; }
    public string? Email { get; init; }
    public string? UserName { get; init; }
    public required string Status { get; init; }
    public bool IsActive => string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase);
}
