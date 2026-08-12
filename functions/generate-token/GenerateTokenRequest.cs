namespace GearFlow.Lambda.GenerateToken;

public sealed class GenerateTokenRequest
{
    public string? ClientId { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
}
