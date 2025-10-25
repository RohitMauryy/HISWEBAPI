using HISWEBAPI.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using HISWEBAPI.GWT.PMS.Exceptions.Log;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeRepository _repository;
        private readonly IDistributedCache _distributedCache;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomeController(IHomeRepository repository, IDistributedCache distributedCache)
        {
            _repository = repository;
            _distributedCache = distributedCache;
        }

        [HttpGet("getActiveBranchList")]
        public async Task<IActionResult> GetActiveBranchListAsync()
        {
            _log.Info("GetActiveBranchListAsync called.");
            try
            {
                var branches = await _repository.GetActiveBranchListAsync();

                if (branches == null || !branches.Any())
                {
                    _log.Warn("No branches found.");
                    return NotFound(new { result = false, message = "No active branches found." });
                }

                _log.Info($"Branches fetched, Count: {branches.Count()}");
                return Ok(new { result = true, data = branches });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }

        [HttpPost("userLogin")]
        public async Task<IActionResult> UserLoginAsync([FromBody] DTO.LoginRequest request)
        {
            _log.Info($"UserLoginAsync called. BranchId={request.BranchId}, UserName={request.UserName}");
            try
            {
                var userId = await _repository.UserLoginAsync(request.BranchId, request.UserName, request.Password);

                if (userId > 0)
                {
                    _log.Info($"Login successful. UserId={userId}");
                    return Ok(new { result = true, userId });
                }

                _log.Warn("Invalid credentials.");
                return Unauthorized(new { result = false, message = "Invalid credentials." });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }
    }
}