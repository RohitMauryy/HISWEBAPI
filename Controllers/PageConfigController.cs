using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HISWEBAPI.DTO;
using HISWEBAPI.Models;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Services;

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

        [HttpPost("createUpdateConfigMaster")]
        [Authorize]
        public IActionResult CreateUpdateConfigMaster([FromBody] PageConfigRequest request)
        {
            _log.Info($"CreateUpdateConfigMaster called. ConfigKey={request?.ConfigKey}, Id={request?.Id}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for page config create/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var globalValues = GetGlobalValues();
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