using System.Security.Claims;

namespace HISWEBAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string username, int hospId, int branchId);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}