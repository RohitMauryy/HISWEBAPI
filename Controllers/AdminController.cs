using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Linq;
using System.Reflection;
using log4net;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using HISWEBAPI.DTO;
using HISWEBAPI.Models;
using HISWEBAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using HISWEBAPI.Repositories.Implementations;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IDistributedCache _distributedCache;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AdminController(
            IAdminRepository repository,
            IDistributedCache distributedCache,
                        IResponseMessageService messageService
            )
        {
            _adminRepository = repository;
            _distributedCache = distributedCache;
            _messageService = messageService;

        }

        private (string Type, string Message) GetAlert(string alertCode)
        {
            return _messageService.GetMessageAndTypeByAlertCode(alertCode);
        }

        // Helper method to extract global values from the authenticated user context
        private AllGlobalValues GetGlobalValues()
        {
            // Extract from JWT claims or session
            var hospIdClaim = User.Claims.FirstOrDefault(c => c.Type == "hospId")?.Value;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            return new AllGlobalValues
            {
                hospId = int.TryParse(hospIdClaim, out int hospId) ? hospId : 0,
                userId = int.TryParse(userIdClaim, out int userId) ? userId : 0,
                ipAddress = ipAddress ?? "Unknown"
            };
        }


        [HttpPost("createUpdateRoleMaster")]
        [Authorize]
        public IActionResult CreateUpdateRoleMaster([FromBody] RoleMasterRequest request)
        {
            _log.Info($"CreateUpdateRoleMaster called.");
            try
            {
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for role insert/update.");
                    return BadRequest(new { result = false, messageType = GetAlert("MODEL_VALIDATION_FAILED").Type, message = GetAlert("MODEL_VALIDATION_FAILED").Message, errors = ModelState });
                }

                // Get global values from claims or session
                var globalValues = GetGlobalValues();

                var jsonResult = _adminRepository.CreateUpdateRoleMaster(request, globalValues);
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResult);

                if (result.result == false)
                {
                    _log.Warn($"Role operation failed: {result.message}");
                    return Conflict(new { result = false, message = result.message.ToString() });
                }

                _log.Info($"Role operation completed successfully: {result.message}");
                return Ok(new { result = true, message = result.message.ToString() });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SERVER_ERROR_FOUND").Type, message = GetAlert("SERVER_ERROR_FOUND").Message });
            }
        }

        [HttpGet("roleMasterList")]
        [Authorize]
        public IActionResult RoleMasterList()
        {
            _log.Info("RoleMasterList called.");
            try
            {
                var roles = _adminRepository.RoleMasterList();

                if (roles == null || !roles.Any())
                {
                    _log.Warn("No roles found.");
                    return NotFound(new { result = false, messageType = GetAlert("DATA_NOT_FOUND").Type, message = GetAlert("DATA_NOT_FOUND").Message });
                }

                _log.Info($"{roles.Count()} roles fetched successfully.");
                return Ok(new { result = true, data = roles });
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                return StatusCode(500, new { result = false, messageType = GetAlert("SERVER_ERROR_FOUND").Type, message = GetAlert("SERVER_ERROR_FOUND").Message });
            }
        }

       
    }
}