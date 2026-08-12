namespace GearFlow.Lambda.GenerateToken;

/// <summary>
/// Equivalente a GearFlow.Application.Settings.JwtSettings (legado).
/// Valores vêm de variáveis de ambiente da Lambda.
/// </summary>
public sealed class JwtSettings
{
    public string Issuer { get; init; } = "GearFlow.Api";
    public string Audience { get; init; } = "GearFlow.Client";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;

    public static JwtSettings FromEnvironment()
    {
        var accessTokenMinutesRaw = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_MINUTES");
        _ = int.TryParse(accessTokenMinutesRaw, out var accessTokenMinutes);

        return new JwtSettings
        {
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "GearFlow.Api",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "GearFlow.Client",
            SigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") ?? string.Empty,
            AccessTokenMinutes = accessTokenMinutes > 0 ? accessTokenMinutes : 30
        };
    }
}
