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

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AdminController(
            IAdminRepository repository,
            IResponseMessageService messageService)
        {
            _adminRepository = repository;
            _messageService = messageService;
        }

        private AllGlobalValues GetGlobalValues()
        {
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
            _log.Info("CreateUpdateRoleMaster called.");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for role insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var globalValues = GetGlobalValues();

            var serviceResult = _adminRepository.CreateUpdateRoleMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Role operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Role operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }



        [HttpGet("roleMasterList")]
        [Authorize]
        public IActionResult RoleMasterList()
        {
            _log.Info("RoleMasterList called.");
            var serviceResult = _adminRepository.RoleMasterList();

            if (serviceResult.Result)
                _log.Info($"Roles fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No roles found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

    }
}
