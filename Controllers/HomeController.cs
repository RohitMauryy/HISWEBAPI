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

        [HttpGet("getActiveBranch")]
        public async Task<ActionResult<IEnumerable<BranchModel>>> GetActiveBranchDetails()
        {
            List<BranchModel> branchDetails = new List<BranchModel>();

            try
            {
                var itemsFromRepo = await _repository.GetActiveBranchListAsync();
                branchDetails = itemsFromRepo.ToList();
                string serializedList = JsonConvert.SerializeObject(branchDetails);

                return Ok(branchDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while fetching Branch Details.",
                    Error = ex.Message
                });
            }
        }



        [HttpPost("login")]
        public async Task<IActionResult> UserLogin([FromBody] LoginModel loginRequest)
        {
            try
            {
                if (loginRequest == null)
                    return BadRequest(new { isSuccess = false, message = "Invalid request." });

                // Call repository method that returns a long (userId or 0 if invalid)
                long userId = await _repository.UserLoginAsync(
                    loginRequest.BranchId,
                    loginRequest.UserName,
                    loginRequest.Password
                );

                if (userId <= 0)
                {
                    return Unauthorized(new
                    {
                        isSuccess = false,
                        message = "Invalid credentials."
                    });
                }

                return Ok(new
                {
                    isSuccess = true,
                    message = "Login successful.",
                    userId = userId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isSuccess = false,
                    message = "An error occurred while logging in.",
                    error = ex.Message
                });
            }
        }


    }
}
