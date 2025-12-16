using Microsoft.AspNetCore.Http;
using HISWEBAPI.Models;
using System.Linq;
using System.Security.Claims;

namespace HISWEBAPI.Configuration
{
    public static class GlobalFunctions
    {

        public static AllGlobalValues GetGlobalValues(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return new AllGlobalValues
                {
                    hospId = 0,
                    userId = 0,
                    userName = "Unknown",
                    name = "Unknown",
                    ipAddress = "Unknown"
                };
            }

            var user = httpContext.User;
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            var hospIdClaim = user.Claims.FirstOrDefault(c => c.Type == "hospId")?.Value;
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var userNameClaim = user.Claims.FirstOrDefault(c => c.Type == "userName")?.Value;
            var nameClaim = user.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            return new AllGlobalValues
            {
                hospId = int.TryParse(hospIdClaim, out int hospId) ? hospId : 0,
                userId = int.TryParse(userIdClaim, out int userId) ? userId : 0,
                userName = userNameClaim ?? "Unknown",
                name = nameClaim ?? "Unknown",
                ipAddress = ipAddress ?? "Unknown"
            };
        }


        public static AllGlobalValues GetGlobalValues(ClaimsPrincipal user, string ipAddress)
        {
            if (user == null)
            {
                return new AllGlobalValues
                {
                    hospId = 0,
                    userId = 0,
                    userName = "Unknown",
                    name = "Unknown",
                    ipAddress = ipAddress ?? "Unknown"
                };
            }

            var hospIdClaim = user.Claims.FirstOrDefault(c => c.Type == "hospId")?.Value;
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var userNameClaim = user.Claims.FirstOrDefault(c => c.Type == "userName")?.Value;
            var nameClaim = user.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            return new AllGlobalValues
            {
                hospId = int.TryParse(hospIdClaim, out int hospId) ? hospId : 0,
                userId = int.TryParse(userIdClaim, out int userId) ? userId : 0,
                userName = userNameClaim ?? "Unknown",
                name = nameClaim ?? "Unknown",
                ipAddress = ipAddress ?? "Unknown"
            };
        }
    }
}