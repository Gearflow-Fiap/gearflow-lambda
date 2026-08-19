using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GearFlow.Lambda.GenerateToken;

/// <summary>
/// Extraído da ideia de AuthTokenHelper.GenerateTokenBundle (legado).
/// </summary>
public static class JwtTokenGenerator
{
    public static (string AccessToken, DateTime ExpiresAtUtc) Generate(
        GenerateTokenRequest request,
        JwtSettings settings)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, request.ClientId!),
            new(JwtRegisteredClaimNames.Sub, request.ClientId!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(request.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, request.Email));

        if (!string.IsNullOrWhiteSpace(request.UserName))
            claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, request.UserName));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (accessToken, expiresAt);
    }
}
