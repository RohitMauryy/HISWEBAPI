using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Reflection;
using log4net;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Exceptions;
using HISWEBAPI.DTO;
using HISWEBAPI.Services;
using HISWEBAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHomeRepository _homeRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomeController(
            IHomeRepository repository,
            IResponseMessageService messageService)
        {
            _homeRepository = repository;
            _messageService = messageService;
        }

        private AllGlobalValues GetGlobalValues()
        {
            var hospIdClaim = User.Claims.FirstOrDefault(c => c.Type == "hospId")?.Value;
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type == "branchId")?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            return new AllGlobalValues
            {
                hospId = int.TryParse(hospIdClaim, out int hospId) ? hospId : 0,
                userId = int.TryParse(userIdClaim, out int userId) ? userId : 0,
                branchId = int.TryParse(branchIdClaim, out int branchId) ? branchId : 0,
                ipAddress = ipAddress ?? "Unknown"
            };
        }


        [HttpGet("getActiveBranchList")]
        [AllowAnonymous]
        public IActionResult GetActiveBranchList()
        {
            _log.Info("GetActiveBranchList called.");
           
                var serviceResult = _homeRepository.GetActiveBranchList();

                if (serviceResult.Result)
                    _log.Info($"Branches fetched: {serviceResult.Message}");
                else
                    _log.Warn($"No branches found: {serviceResult.Message}");

                return StatusCode(serviceResult.StatusCode, new
                {
                    result = serviceResult.Result,
                    messageType = serviceResult.MessageType,
                    message = serviceResult.Message,
                    data = serviceResult.Data
                });
        }

        
        [HttpGet("getPickListMaster")]
        [AllowAnonymous]
        public IActionResult GetPickListMaster([FromQuery] string fieldName)
        {
            _log.Info($"GetPickListMaster called with fieldName: {fieldName}");
           
                var serviceResult = _homeRepository.GetPickListMaster(fieldName);

                if (serviceResult.Result)
                    _log.Info($"PickList fetched: {serviceResult.Message}");
                else
                    _log.Warn($"No PickList found: {serviceResult.Message}");

                return StatusCode(serviceResult.StatusCode, new
                {
                    result = serviceResult.Result,
                    messageType = serviceResult.MessageType,
                    message = serviceResult.Message,
                    data = serviceResult.Data
                });
            
        }

        [HttpPost("createUpdateResponseMessage")]
        [Authorize]
        public IActionResult CreateUpdateResponseMessage([FromBody] ResponseMessageRequest request)
        {
            _log.Info("CreateUpdateResponseMessage called.");
           
                if (!ModelState.IsValid)
                {
                    _log.Warn("Invalid model state for Response message insert/update.");
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

                var jsonResult = _messageService.CreateUpdateResponseMessage(request, globalValues);
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResult);

                if (result.result == false)
                {
                    _log.Warn($"Response message operation failed: {result.message}");
                    return Conflict(new
                    {
                        result = false,
                        messageType = result.messageType?.ToString() ?? "Error",
                        message = result.message.ToString()
                    });
                }

                _log.Info($"Response message operation completed: {result.message}");
                return Ok(new
                {
                    result = true,
                    messageType = result.messageType?.ToString() ?? "Info",
                    message = result.message.ToString()
                });
          
        }
    }
}