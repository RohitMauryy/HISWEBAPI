using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;
using System.Reflection;
using log4net;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using HISWEBAPI.DTO;
using HISWEBAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeRepository _repository;
        private readonly IDistributedCache _distributedCache;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomeController(
            IHomeRepository repository,
            IDistributedCache distributedCache
            )
        {
            _repository = repository;
            _distributedCache = distributedCache;
        }

        [HttpGet("getActiveBranchList")]
        [AllowAnonymous]
        public IActionResult GetActiveBranchList()
        {
            _log.Info("GetActiveBranchList called.");
            try
            {
                var branches = _repository.GetActiveBranchList();
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


    }

   
}