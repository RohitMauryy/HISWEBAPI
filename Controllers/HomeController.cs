using HISWEBAPI.Models;
using HISWEBAPI.Interface;
using HISWEBAPI.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using HISWEBAPI.GWT.PMS.Exceptions.Log;
using Microsoft.AspNetCore.Identity.Data;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeRepository _repository;
        private readonly IDistributedCache _distributedCache;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomeController(IHomeRepository repository, IDistributedCache distributedCache)
        {
            _repository = repository;
            _distributedCache = distributedCache;
        }

        [HttpGet("getActiveBranchList")]
        public async Task<IActionResult> GetActiveBranchListAsync()
        {
            log.Info("GetActiveBranchListAsync called in controller.");

            try
            {
                var branches = await _repository.GetActiveBranchListAsync();

                if (branches == null)
                {
                    log.Warn("No branches found.");
                    return NotFound("No active branches found.");
                }

              
                log.Info($"Branches fetched, Count: {branches.Count()}");
                return Ok(branches);
            }
            catch (Exception ex)
            {
                log.Error("Exception in GetActiveBranchListAsync controller.", ex);
                LogErrors.writeErrorLog(ex, $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> UserLoginAsync([FromBody] DTO.LoginRequest request)
        {
            log.Info($"UserLoginAsync called. BranchId={request.BranchId}, UserName={request.UserName}");

            try
            {
                var userId = await _repository.UserLoginAsync(request.BranchId, request.UserName, request.Password);

                if (userId > 0)
                {
                    log.Info($"User login successful. UserId={userId}");
                    return Ok(new { result = true, userId });
                }
                else if (userId == 0)
                {
                    log.Warn("Invalid credentials.");
                    return Unauthorized(new { result = false, message = "Invalid credentials." });
                }
                else
                {
                    log.Error("Internal error during login.");
                    return StatusCode(500, new { result = false, message = "Internal server error." });
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in UserLoginAsync controller.", ex);
                LogErrors.writeErrorLog(ex, $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = ex.Message });
            }
        }

    }
}
