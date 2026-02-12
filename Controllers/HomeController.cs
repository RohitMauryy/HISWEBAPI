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
using HISWEBAPI.Configuration;
using HISWEBAPI.Repositories.Implementations;
using System.Text.RegularExpressions;

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


        [HttpPost("clearAllCache")]
        [Authorize]
        public IActionResult ClearAllCache()
        {
            _log.Info("ClearAllCache API endpoint called.");


            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

            var serviceResult = _homeRepository.ClearAllCache();

            if (serviceResult.Result)
            {
                _log.Info($"Cache cleared successfully by UserId={globalValues.userId} from IP={globalValues.ipAddress}");
            }
            else
            {
                _log.Warn($"Cache clearing failed: {serviceResult.Message}");
            }

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });

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

                var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

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

        [HttpGet("getAllGlobalValues")]
        [Authorize]
        public IActionResult GetAllGlobalValues()
        {
            _log.Info("GetAllGlobalValues endpoint called.");

                var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

                _log.Info($"Global values retrieved: HospId={globalValues.hospId}, UserId={globalValues.userId}, IpAddress={globalValues.ipAddress}");

                var alert = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");

                return Ok(new
                {
                    result = true,
                    messageType = alert.Type,
                    message = alert.Message,
                    data = globalValues
                });
            
           
        }


        [HttpGet("getCountryMaster")]
        [Authorize]
        public IActionResult GetCountryMaster([FromQuery] int? isActive = null)
        {
            _log.Info($"GetCountryMaster called. IsActive={isActive?.ToString() ?? "All"}");

            var serviceResult = _homeRepository.GetCountryMaster(isActive);

            if (serviceResult.Result)
                _log.Info($"Countries fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No countries found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getStateMaster")]
        [Authorize]
        public IActionResult GetStateMaster([FromQuery] int countryId, [FromQuery] int? isActive = null)
        {
            _log.Info($"GetStateMaster called. CountryId={countryId}, IsActive={isActive?.ToString() ?? "All"}");

            if (countryId <= 0)
            {
                _log.Warn("Invalid CountryId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "CountryId must be greater than 0",
                    errors = new { countryId }
                });
            }

            var serviceResult = _homeRepository.GetStateMaster(countryId, isActive);

            if (serviceResult.Result)
                _log.Info($"States fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No states found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getDistrictMaster")]
        [Authorize]
        public IActionResult GetDistrictMaster([FromQuery] int stateId, [FromQuery] int? isActive = null)
        {
            _log.Info($"GetDistrictMaster called. StateId={stateId}, IsActive={isActive?.ToString() ?? "All"}");

            if (stateId <= 0)
            {
                _log.Warn("Invalid StateId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "StateId must be greater than 0",
                    errors = new { stateId }
                });
            }

            var serviceResult = _homeRepository.GetDistrictMaster(stateId, isActive);

            if (serviceResult.Result)
                _log.Info($"Districts fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No districts found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getCityMaster")]
        [Authorize]
        public IActionResult GetCityMaster([FromQuery] int districtId, [FromQuery] int? isActive = null)
        {
            _log.Info($"GetCityMaster called. DistrictId={districtId}, IsActive={isActive?.ToString() ?? "All"}");

            if (districtId <= 0)
            {
                _log.Warn("Invalid DistrictId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "DistrictId must be greater than 0",
                    errors = new { districtId }
                });
            }

            var serviceResult = _homeRepository.GetCityMaster(districtId, isActive);

            if (serviceResult.Result)
                _log.Info($"Cities fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No cities found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpGet("getPincodeMaster")]
        [Authorize]
        public IActionResult GetPincodeMaster([FromQuery] int cityId, [FromQuery] int? isActive = null)
        {
            _log.Info($"GetPincodeMaster called. CityId={cityId}, IsActive={isActive?.ToString() ?? "All"}");

            if (cityId <= 0)
            {
                _log.Warn("Invalid CityId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "CityId must be greater than 0",
                    errors = new { cityId }
                });
            }

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

            var serviceResult = _homeRepository.GetPincodeMaster(cityId, isActive);

            if (serviceResult.Result)
                _log.Info($"Pincodes fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No pincodes found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpGet("getLocationByPincode")]
        [Authorize]
        public IActionResult GetLocationByPincode([FromQuery] int pincode)
        {
            _log.Info($"GetLocationByPincode called. Pincode={pincode}");

            // Validate pincode format (6 digits)
            if (pincode < 100000 || pincode > 999999)
            {
                _log.Warn($"Invalid pincode format: {pincode}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Pincode must be exactly 6 digits",
                    errors = new { pincode }
                });
            }

            var serviceResult = _homeRepository.GetLocationByPincode(pincode);

            if (serviceResult.Result)
                _log.Info($"Location fetched successfully for pincode: {pincode}");
            else
                _log.Warn($"Location fetch failed for pincode: {pincode}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }




        [HttpGet("getAllInsuranceCompanyList")]
        [Authorize]
        public IActionResult GetAllInsuranceCompanyList()
        {
            _log.Info("GetAllInsuranceCompanyList API called.");

            var serviceResult = _homeRepository.GetAllInsuranceCompanyList();

            if (serviceResult.Result)
                _log.Info($"Insurance companies fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No insurance companies found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

      
        [HttpGet("getCorporateListByInsuranceCompanyId")]
        [Authorize]
        public IActionResult GetCorporateListByInsuranceCompanyId(
            [FromQuery] int? insuranceCompanyId,
            [FromQuery] int? isActive = null)
        {
            _log.Info($"GetCorporateListByInsuranceCompanyId API called. InsuranceCompanyId={insuranceCompanyId?.ToString() ?? "All"}, IsActive={isActive?.ToString() ?? "All"}");

            if (insuranceCompanyId == null || insuranceCompanyId < 0)
            {
                _log.Warn("Invalid insuranceCompanyId supplied.");
                return BadRequest(new
                {
                    result = false,
                    messageType = "ERROR",
                    message = "insuranceCompanyId is mandatory and must be greater than equal to 0.",
                    data = ""
                });
            }

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

            var serviceResult = _homeRepository.GetCorporateListByInsuranceCompanyId(
                insuranceCompanyId,
                isActive);

            if (serviceResult.Result)
                _log.Info($"Corporates fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No corporates found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }



        [HttpGet("getFile")]
        [Authorize] 
        public IActionResult GetFile([FromQuery] string filePath)
        {
            _log.Info($"GetFile called. FilePath={filePath}");

            if (string.IsNullOrWhiteSpace(filePath))
            {
                _log.Warn("File path is null or empty");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "File path is required",
                    errors = new { filePath = "File path cannot be empty" }
                });
            }

            var serviceResult = _homeRepository.GetFile(filePath);

            if (serviceResult.Result)
            {
                _log.Info($"File retrieved successfully: {serviceResult.Message}");
                return File(
                    serviceResult.Data.FileStream,
                    serviceResult.Data.ContentType,
                    serviceResult.Data.FileName
                );
            }
            else
            {
                _log.Warn($"File retrieval failed: {serviceResult.Message}");
                return StatusCode(serviceResult.StatusCode, new
                {
                    result = serviceResult.Result,
                    messageType = serviceResult.MessageType,
                    message = serviceResult.Message
                });
            }
        }

        [HttpGet("getFileAsBase64")]
        [Authorize]
        public IActionResult GetFileAsBase64([FromQuery] string filePath)
        {
            _log.Info($"GetFileAsBase64 called. FilePath={filePath}");

            if (string.IsNullOrWhiteSpace(filePath))
            {
                _log.Warn("File path is null or empty");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "File path is required"
                });
            }

            var serviceResult = _homeRepository.GetFileAsBase64(filePath);

            if (serviceResult.Result)
                _log.Info($"File retrieved as base64 successfully: {serviceResult.Message}");
            else
                _log.Warn($"File retrieval as base64 failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("fileExists")]
        [Authorize]
        public IActionResult FileExists([FromQuery] string filePath)
        {
            _log.Info($"FileExists called. FilePath={filePath}");

            if (string.IsNullOrWhiteSpace(filePath))
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "File path is required"
                });
            }

            var serviceResult = _homeRepository.CheckFileExists(filePath);

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getDoctorMasterListByBranchId")]
        [Authorize]
        public IActionResult GetDoctorMasterListByBranchId(
      [FromQuery] int branchId,
      [FromQuery] int? departmentId = null,
      [FromQuery] int? specializationId = null,
      [FromQuery] byte? isDoctorUnit = null)
        {
            _log.Info($"GetDoctorMasterListByBranchId called. BranchId={branchId}, DepartmentId={departmentId?.ToString() ?? "All"}, SpecializationId={specializationId?.ToString() ?? "All"}, IsDoctorUnit={isDoctorUnit?.ToString() ?? "All"}");

            if (branchId <= 0)
            {
                _log.Warn("Invalid BranchId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId must be greater than 0",
                    errors = new { branchId }
                });
            }

            var serviceResult = _homeRepository.GetDoctorMasterListByBranchId(
                branchId,
                departmentId,
                specializationId,
                isDoctorUnit);

            if (serviceResult.Result)
                _log.Info($"Doctors fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No doctors found: {serviceResult.Message}");

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