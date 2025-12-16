using System.Security.Claims;

namespace HISWEBAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(int userId, string userName, string name, int hospId);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}