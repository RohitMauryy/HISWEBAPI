using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HISWEBAPI.Configuration;

namespace HISWEBAPI.Services
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string username, string email, List<string>? roles = null);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}