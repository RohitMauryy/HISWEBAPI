using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HISWEBAPI.DTO;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;
using HISWEBAPI.Models.Configuration;

namespace HISWEBAPI.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class PageConfigController : ControllerBase
    {
        private readonly IPageConfigRepository _pageConfigRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PageConfigController(
            IPageConfigRepository pageConfigRepository,
            IResponseMessageService messageService)
        {
            _pageConfigRepository = pageConfigRepository;
            _messageService = messageService;
        }

      

        [HttpPost("createUpdateConfigMaster")]
        [Authorize]
        public IActionResult CreateUpdateConfigMaster(
           [FromQuery] int id,
           [FromQuery] string configKey,
           [FromBody] string configJson)
        {
            _log.Info($"CreateUpdateConfigMaster called. ConfigKey={configKey}, Id={id}");

            // Manual validation since parameters are separated
            if (string.IsNullOrWhiteSpace(configKey))
            {
                _log.Warn("ConfigKey is missing or empty.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "ConfigKey is required",
                    errors = new[] { "ConfigKey cannot be empty" }
                });
            }

            if (configKey.Length > 256)
            {
                _log.Warn($"ConfigKey exceeds maximum length: {configKey.Length}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "ConfigKey cannot exceed 256 characters",
                    errors = new[] { $"ConfigKey length: {configKey.Length}, Maximum: 256" }
                });
            }

            if (string.IsNullOrWhiteSpace(configJson))
            {
                _log.Warn("ConfigJson is missing or empty.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "ConfigJson is required",
                    errors = new[] { "ConfigJson cannot be empty" }
                });
            }

            // Create request object
            var request = new PageConfigRequest
            {
                Id = id,
                ConfigKey = configKey,
                ConfigJson = configJson,
                IsActive = true
            };

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _pageConfigRepository.CreateUpdatePageConfig(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"PageConfig operation completed: {serviceResult.Message}");
            else
                _log.Warn($"PageConfig operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpGet("getConfigMaster")]
        [Authorize]
        public IActionResult GetConfigMaster([FromQuery] string configKey = null)
        {
            _log.Info($"GetConfigMaster called. ConfigKey={configKey ?? "All"}");

            var serviceResult = _pageConfigRepository.GetPageConfig(configKey);

            if (serviceResult.Result)
                _log.Info($"PageConfig fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"PageConfig fetch failed: {serviceResult.Message}");

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