using Microsoft.AspNetCore.Http;
using HISWEBAPI.Models;
using System.Linq;
using System.Security.Claims;

namespace HISWEBAPI.Models.Configuration
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
                    branchId = 0,
                    ipAddress = "Unknown"
                };
            }

            var user = httpContext.User;
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            var hospIdClaim = user.Claims.FirstOrDefault(c => c.Type == "hospId")?.Value;
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var branchIdClaim = user.Claims.FirstOrDefault(c => c.Type == "branchId")?.Value;

            return new AllGlobalValues
            {
                hospId = int.TryParse(hospIdClaim, out int hospId) ? hospId : 0,
                userId = int.TryParse(userIdClaim, out int userId) ? userId : 0,
                branchId = int.TryParse(branchIdClaim, out int branchId) ? branchId : 0,
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
                    branchId = 0,
                    ipAddress = ipAddress ?? "Unknown"
                };
            }

            var hospIdClaim = user.Claims.FirstOrDefault(c => c.Type == "hospId")?.Value;
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var branchIdClaim = user.Claims.FirstOrDefault(c => c.Type == "branchId")?.Value;

            return new AllGlobalValues
            {
                hospId = int.TryParse(hospIdClaim, out int hospId) ? hospId : 0,
                userId = int.TryParse(userIdClaim, out int userId) ? userId : 0,
                branchId = int.TryParse(branchIdClaim, out int branchId) ? branchId : 0,
                ipAddress = ipAddress ?? "Unknown"
            };
        }
    }
}