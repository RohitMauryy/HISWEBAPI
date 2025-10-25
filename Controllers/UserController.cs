using HISWEBAPI.DTO;
using HISWEBAPI.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;
using log4net;
using HISWEBAPI.GWT.PMS.Exceptions.Log;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("insertUserMaster")]
        public IActionResult InsertUserMaster([FromBody] UserMasterRequest request)
        {
            _log.Info($"InsertUserMaster called. UserName={request.UserName}");

            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for user insert.");
                    return BadRequest(new { result = false, message = "Invalid input data.", errors = ModelState });
                }

                var result = _userRepository.InsertUserMaster(request);

                if (result == -2)
                {
                    _log.Warn("License validation failed for user insert.");
                    return BadRequest(new { result = false, message = "License limit reached. Cannot add more users." });
                }

                if (result > 0)
                {
                    _log.Info($"User inserted successfully. UserId={result}");
                    return Ok(new { result = true, userId = result, message = "User created successfully." });
                }

                _log.Error("Failed to insert user. Result=0");
                return StatusCode(500, new { result = false, message = "Failed to create user." });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }
    }
}
