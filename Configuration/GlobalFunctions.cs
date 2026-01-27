using Microsoft.AspNetCore.Http;
using HISWEBAPI.Models;
using System.Linq;
using System.Security.Claims;
using StackExchange.Redis;
using log4net;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace HISWEBAPI.Configuration
{
    public static class GlobalFunctions
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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

        /// <summary>
        /// Clear cache using pattern matching
        /// Example: GlobalFunctions.ClearCacheByPattern(configuration, "_UserWiseMenuMapping_*")
        /// </summary>
        /// <param name="configuration">IConfiguration instance</param>
        /// <param name="pattern">Redis key pattern (supports wildcards like *)</param>
        /// <returns>Number of cache entries cleared</returns>
        public static int ClearCacheByPattern(IConfiguration configuration, string pattern)
        {
            try
            {
                var redisConnection = configuration.GetValue<string>("Redis:Configuration") ?? "localhost:6379";

                using (var redis = ConnectionMultiplexer.Connect(redisConnection))
                {
                    var server = redis.GetServer(redis.GetEndPoints().First());
                    var db = redis.GetDatabase();

                    // Get all keys matching the pattern
                    var keys = server.Keys(pattern: pattern).ToList();

                    if (!keys.Any())
                    {
                        _log.Info($"No cache keys found matching pattern: {pattern}");
                        return 0;
                    }

                    int clearedCount = 0;
                    foreach (var key in keys)
                    {
                        try
                        {
                            db.KeyDelete(key);
                            _log.Info($"Cleared cache key: {key}");
                            clearedCount++;
                        }
                        catch (Exception ex)
                        {
                            _log.Warn($"Failed to clear cache key '{key}': {ex.Message}");
                        }
                    }

                    _log.Info($"Cleared {clearedCount} cache entries matching pattern: {pattern}");
                    return clearedCount;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error clearing cache by pattern '{pattern}': {ex.Message}", ex);
                return 0;
            }
        }
    }
}

/*
===========================================
USAGE EXAMPLES IN ANY REPOSITORY:
===========================================

// 1. In Repository Constructor - Add IConfiguration:
private readonly IConfiguration _configuration;

public AdminRepository(

    IConfiguration configuration)
{
  
    _configuration = configuration;
}


// 2. Usage in SaveUpdateUserMenuMaster:
GlobalFunctions.ClearCacheByPattern(_configuration, $"_UserWiseMenuMapping_*");
GlobalFunctions.ClearCacheByPattern(_configuration, $"_UserTabMenu_*");

// 3. Clear specific user's cache:
GlobalFunctions.ClearCacheByPattern(_configuration, $"_UserWiseMenuMapping_{branchId}_{typeId}_{userId}_*");

// 4. Clear all role menu mappings:
GlobalFunctions.ClearCacheByPattern(_configuration, "_RoleWiseMenuMapping_*");

// 5. Clear specific role mapping:
GlobalFunctions.ClearCacheByPattern(_configuration, $"_RoleWiseMenuMapping_{branchId}_{roleId}");

// 6. Clear all cache (use with caution):
GlobalFunctions.ClearCacheByPattern(_configuration, "*");

// 7. Clear multiple patterns:
GlobalFunctions.ClearCacheByPattern(_configuration, "_UserMaster_*");
GlobalFunctions.ClearCacheByPattern(_configuration, "_RoleMaster_*");
GlobalFunctions.ClearCacheByPattern(_configuration, "_BranchMaster_*");
*/
