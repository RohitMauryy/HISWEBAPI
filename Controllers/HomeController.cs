using Microsoft.AspNetCore.Mvc;
using HISWEBAPI.Models;
using HISWEBAPI.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeRepository _repository;
        private readonly IDistributedCache _distributedCache;

        public HomeController(IHomeRepository repository, IDistributedCache distributedCache)
        {
            _repository = repository;
            _distributedCache = distributedCache;
        }

        [HttpPost("login")]
        public async Task<IActionResult> UserLogin([FromBody] LoginModel loginRequest)
        {
            try
            {
                if (loginRequest == null)
                    return BadRequest(new {  Message = "Invalid request." });

                var loginDetails = await _repository.UserLoginAsync(
                    loginRequest.BranchId,
                    loginRequest.UserName,
                    loginRequest.Password
                );

                if (loginDetails == null || !loginDetails.Any())
                    return Unauthorized(new { result = false, Message = "Invalid credentials." });

                return Ok(new { result = true, Message = "Login successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while logging in.",
                    Error = ex.Message
                });
            }
        }

    }
}
