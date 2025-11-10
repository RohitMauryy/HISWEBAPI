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
using HISWEBAPI.Repositories.Implementations;

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

        [HttpGet("getFaIconList")]
        [Authorize]
        public IActionResult getFaIconMaster()
        {
            _log.Info("getFaIconList called.");
            var serviceResult = _adminRepository.getFaIconMaster();

            if (serviceResult.Result)
                _log.Info($"icon fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No icon found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("CreateUpdateUserMaster")]
        [Authorize]
        public IActionResult CreateUpdateUserMaster([FromBody] UserMasterRequest request)
        {
            _log.Info($"CreateUpdateUserMaster called. UserName={request.UserName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for CreateUpdateUserMaster.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            var serviceResult = _adminRepository.CreateUpdateUserMaster(request);

            if (serviceResult.Result)
                _log.Info($"CreateUpdateUserMaster successful: {serviceResult.Message}");
            else
                _log.Warn($"CreateUpdateUserMaster failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpGet("userMasterList")]
        [Authorize]
        public IActionResult UserMasterList()
        {
            _log.Info("UserMasterList called.");
            var serviceResult = _adminRepository.UserMasterList();
            if (serviceResult.Result)
                _log.Info($"Users fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No users found: {serviceResult.Message}");
            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("createUpdateUserDepartment")]
        [Authorize]
        public IActionResult CreateUpdateUserDepartment([FromBody] UserDepartmentRequest request)
        {
            _log.Info("CreateUpdateUserDepartment called.");
            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for department insert/update.");
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
            var serviceResult = _adminRepository.CreateUpdateUserDepartment(request, globalValues);
            if (serviceResult.Result)
                _log.Info($"Department operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Department operation failed: {serviceResult.Message}");
            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("userDepartmentList")]
        [Authorize]
        public IActionResult UserDepartmentList()
        {
            _log.Info("UserDepartmentList called.");
            var serviceResult = _adminRepository.UserDepartmentList();
            if (serviceResult.Result)
                _log.Info($"Departments fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No departments found: {serviceResult.Message}");
            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("createUpdateUserGroupMaster")]
        [Authorize]
        public IActionResult CreateUpdateUserGroupMaster([FromBody] UserGroupRequest request)
        {
            _log.Info("CreateUpdateUserGroupMaster called.");
            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for group insert/update.");
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
            var serviceResult = _adminRepository.CreateUpdateUserGroupMaster(request, globalValues);
            if (serviceResult.Result)
                _log.Info($"Group operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Group operation failed: {serviceResult.Message}");
            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("userGroupList")]
        [Authorize]
        public IActionResult UserGroupList()
        {
            _log.Info("UserGroupList called.");
            var serviceResult = _adminRepository.UserGroupList();
            if (serviceResult.Result)
                _log.Info($"Groups fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No groups found: {serviceResult.Message}");
            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("createUpdateUserGroupMembers")]
        [Authorize]
        public IActionResult CreateUpdateUserGroupMembers([FromBody] UserGroupMembersRequest request)
        {
            _log.Info("CreateUpdateUserGroupMembers called.");
            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for group members insert/update.");
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
            var serviceResult = _adminRepository.CreateUpdateUserGroupMembers(request, globalValues);
            if (serviceResult.Result)
                _log.Info($"Group members operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Group members operation failed: {serviceResult.Message}");
            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("userGroupMembersList")]
        [Authorize]
        public IActionResult UserGroupMembersList([FromQuery] int? groupId = null)
        {
            _log.Info("UserGroupMembersList called.");
            var serviceResult = _adminRepository.UserGroupMembersList(groupId);
            if (serviceResult.Result)
                _log.Info($"Group members fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No group members found: {serviceResult.Message}");
            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }



      

        [HttpPost("saveUpdateRoleMapping")]
        [Authorize]
        public IActionResult SaveUpdateRoleMapping([FromBody] UserRoleMappingListRequest request)
        {
            _log.Info("SaveUpdateRoleMapping called.");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for user role mapping save/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate that all items (if any exist) have the same userId, branchId, and typeId as the parent request
            if (request.userRoleMappings != null && request.userRoleMappings.Count > 0)
            {
                bool isConsistent = request.userRoleMappings.All(x =>
                    x.userId == request.userId &&
                    x.branchId == request.branchId &&
                    x.typeId == request.typeId);

                if (!isConsistent)
                {
                    _log.Warn("Inconsistent userId, branchId, or typeId in role mapping list.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "All role mapping items must have the same userId, branchId, and typeId as the request"
                    });
                }

                _log.Info($"Saving user role mapping for UserId={request.userId}, BranchId={request.branchId}, TypeId={request.typeId}, Count={request.userRoleMappings.Count}");
            }
            else
            {
                _log.Info($"Removing all roles for UserId={request.userId}, BranchId={request.branchId}, TypeId={request.typeId}");
            }

            var globalValues = GetGlobalValues();

            var serviceResult = _adminRepository.SaveUpdateRoleMapping(
                request.userId,
                request.branchId,
                request.typeId,
                request.userRoleMappings ?? new List<UserRoleMappingRequest>(),
                globalValues
            );

            if (serviceResult.Result)
                _log.Info($"User role mapping saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"User role mapping save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getAssignRoleForUserAuthorization")]
        [Authorize]
        public IActionResult GetAssignRoleForUserAuthorization([FromQuery] int branchId, [FromQuery] int typeId, [FromQuery] int userId)
        {
            _log.Info($"GetAssignRoleForUserAuthorization called. BranchId={branchId}, TypeId={typeId}, UserId={userId}");

            if (branchId <= 0 || typeId <= 0 || userId <= 0)
            {
                _log.Warn("Invalid parameters for GetAssignRoleForUserAuthorization.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId, TypeId, and UserId must be greater than 0"
                });
            }

            var serviceResult = _adminRepository.GetAssignRoleForUserAuthorization(branchId, typeId, userId);

            if (serviceResult.Result)
                _log.Info($"Role authorization data fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No role authorization data found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

      
        [HttpPost("saveUpdateUserRightMapping")]
        [Authorize]
        public IActionResult SaveUpdateUserRightMapping([FromBody] SaveUserRightMappingRequest request)
        {
            _log.Info($"SaveUpdateUserRightMapping called. TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, RoleId={request.RoleId}, UserRights Count={request.UserRights.Count}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveUpdateUserRightMapping.");
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

            var serviceResult = _adminRepository.SaveUpdateUserRightMapping(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"User right mapping saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"User right mapping save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getAssignUserRightMapping")]
        [Authorize]
        public IActionResult GetAssignUserRightMapping(
          [FromQuery] int branchId,
          [FromQuery] int typeId,
          [FromQuery] int userId,
          [FromQuery] int roleId)
        {
            _log.Info($"GetAssignUserRightMapping called. BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

            if (branchId <= 0 || typeId <= 0 || userId <= 0 || roleId <= 0)
            {
                _log.Warn("Invalid parameters for GetAssignUserRightMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, typeId, userId, roleId) must be greater than 0",
                    errors = new { branchId, typeId, userId, roleId }
                });
            }

            var serviceResult = _adminRepository.GetAssignUserRightMapping(branchId, typeId, userId, roleId);

            if (serviceResult.Result)
                _log.Info($"User right mapping fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"User right mapping fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }




      
        [HttpPost("saveUpdateDashBoardUserRightMapping")]
        [Authorize]
        public IActionResult SaveUpdateDashBoardUserRightMapping([FromBody] SaveDashboardUserRightMappingRequest request)
        {
            _log.Info($"SaveUpdateDashBoardUserRightMapping called. TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, RoleId={request.RoleId}, DashboardUserRights Count={request.DashboardUserRights.Count}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveUpdateDashBoardUserRightMapping.");
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

            var serviceResult = _adminRepository.SaveUpdateDashBoardUserRightMapping(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Dashboard user right mapping saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"Dashboard user right mapping save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpGet("getAssignDashBoardUserRight")]
        [Authorize]
        public IActionResult GetAssignDashBoardUserRight(
              [FromQuery] int branchId,
              [FromQuery] int typeId,
              [FromQuery] int userId,
              [FromQuery] int roleId)
        {
            _log.Info($"GetAssignDashBoardUserRight called. BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

            if (branchId <= 0 || typeId <= 0 || userId <= 0 || roleId <= 0)
            {
                _log.Warn("Invalid parameters for GetAssignDashBoardUserRight.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, typeId, userId, roleId) must be greater than 0",
                    errors = new { branchId, typeId, userId, roleId }
                });
            }

            var serviceResult = _adminRepository.GetAssignDashBoardUserRight(branchId, typeId, userId, roleId);

            if (serviceResult.Result)
                _log.Info($"Dashboard user right mapping fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Dashboard user right mapping fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("createUpdateNavigationTabMaster")]
        [Authorize]
        public IActionResult CreateUpdateNavigationTabMaster([FromBody] NavigationTabMasterRequest request)
        {
            _log.Info("CreateUpdateNavigationTabMaster called.");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for navigation tab insert/update.");
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

            var serviceResult = _adminRepository.CreateUpdateNavigationTabMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Navigation tab operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Navigation tab operation failed: {serviceResult.Message}");

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
