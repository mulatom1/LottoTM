using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LottoTM.Server.Api.Services;

/// <summary>
/// Interface for JWT token generation service
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a JWT token for authenticated user
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="email">User's email address</param>
    /// <param name="isAdmin">Admin privilege flag</param>
    /// <param name="expiresAt">Output parameter - token expiration timestamp</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(int userId, string email, bool isAdmin, out DateTime expiresAt);
}

/// <summary>
/// Service for generating JWT tokens for authentication
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public string GenerateToken(int userId, string email, bool isAdmin, out DateTime expiresAt)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];
        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 1440);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var issuedAt = DateTime.UtcNow;
        expiresAt = issuedAt.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("isAdmin", isAdmin.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
