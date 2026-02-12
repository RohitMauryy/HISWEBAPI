using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using log4net;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.DTO;
using HISWEBAPI.Services;
using HISWEBAPI.Configuration;
using MimeKit.Encodings;

namespace HISWEBAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : ControllerBase
    {
        private readonly IStoreRepository _storeRepository;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public StoreController(
            IStoreRepository repository,
            IResponseMessageService messageService)
        {
            _storeRepository = repository;
            _messageService = messageService;
        }

        [HttpPost("createUpdateVendorMaster")]
        [Authorize]
        public IActionResult CreateUpdateVendorMaster([FromBody] CreateUpdateVendorMasterRequest request)
        {
            _log.Info($"CreateUpdateVendorMaster called. VendorName={request.VendorName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for vendor master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate pincode format (6 digits)
            if (request.Pincode < 100000 || request.Pincode > 999999)
            {
                _log.Warn($"Invalid pincode format: {request.Pincode}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Pincode must be exactly 6 digits",
                    errors = new { request.Pincode }
                });
            }

            // Additional validation for cityid, DistrictId, StateId, and CountryId
            if (request.CityId <= 0)
            {
                _log.Warn("Invalid CityId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "CityId must be greater than 0",
                    errors = new { cityId = request.CityId }
                });
            }
            if (request.DistrictId <= 0)
            {
                _log.Warn("Invalid DistrictId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "DistrictId must be greater than 0",
                    errors = new { districtId = request.DistrictId }
                });
            }

            if (request.StateId <= 0)
            {
                _log.Warn("Invalid StateId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "StateId must be greater than 0",
                    errors = new { stateId = request.StateId }
                });
            }

            if (request.CountryId <= 0)
            {
                _log.Warn("Invalid CountryId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "CountryId must be greater than 0",
                    errors = new { countryId = request.CountryId }
                });
            }


            // Validate IsActive value
            if (request.IsActive != 0 && request.IsActive != 1)
            {
                _log.Warn("Invalid IsActive value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 or 1",
                    errors = new { isActive = request.IsActive }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _storeRepository.CreateUpdateVendorMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Vendor master operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Vendor master operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getVendorMasterList")]
        [Authorize]
        public IActionResult GetVendorMasterList(
            [FromQuery] int? vendorId = null,
            [FromQuery] int? isActive = null)
        {
            _log.Info($"GetVendorMasterList called. VendorId={vendorId?.ToString() ?? "All"}, IsActive={isActive?.ToString() ?? "All"}");

            // Validate IsActive parameter if provided
            if (isActive.HasValue && isActive.Value != 0 && isActive.Value != 1)
            {
                _log.Warn($"Invalid IsActive parameter: {isActive.Value}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 (Inactive), 1 (Active), or null (All)",
                    errors = new { isActive }
                });
            }

            var serviceResult = _storeRepository.GetVendorMasterList(vendorId, isActive);

            if (serviceResult.Result)
                _log.Info($"Vendors fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No vendors found: {serviceResult.Message}");

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