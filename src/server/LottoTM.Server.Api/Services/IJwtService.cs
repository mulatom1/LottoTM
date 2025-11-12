namespace LottoTM.Server.Api.Services;

/// <summary>
/// Interface for JWT token generation service
/// </summary>
public interface IJwtService
{
    string GenerateToken(int userId, string email, bool isAdmin, out DateTime expiresAt);

    Task<int> GetUserIdFromJwt();
    Task<string> GetEmailFromJwt();
    Task<bool> GetIsAdminFromJwt();
}
