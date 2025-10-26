using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;
using log4net;
using HISWEBAPI.DTO.User;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;

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

                // Handle duplicate username
                if (result == -1)
                {
                    _log.Warn($"Duplicate username attempted: {request.UserName}");
                    return Conflict(new { result = false, message = "Username already exists. Please choose a different username." });
                }

                // Handle license limit
                if (result == -2)
                {
                    _log.Warn("License validation failed for user insert.");
                    return BadRequest(new { result = false, message = "License limit reached. Cannot add more users." });
                }

                if (result > 0)
                {
                    _log.Info($"User inserted/updated successfully. UserId={result}");
                    string message = request.UserId.HasValue && request.UserId.Value > 0
                        ? "User updated successfully."
                        : "User created successfully.";

                    return Ok(new { result = true, userId = result, message });
                }

                _log.Error("Failed to insert/update user. Result=0");
                return StatusCode(500, new { result = false, message = "Failed to save user." });
            }
            catch (Exception ex)
            {
                _log.Error($"Error in InsertUserMaster: {ex.Message}", ex);
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, message = "Server error occurred." });
            }
        }
    }
}