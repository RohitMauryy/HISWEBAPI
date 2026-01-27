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
using HISWEBAPI.Configuration;

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

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
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



        [HttpPatch("updateRoleMasterStatus")]
        [Authorize]
        public IActionResult UpdateRoleMasterStatus([FromQuery] int roleId, [FromQuery] int isActive)
        {
            _log.Info($"UpdateRoleMasterStatus called. RoleId={roleId}, IsActive={isActive}");

            if (roleId <= 0)
            {
                _log.Warn("Invalid RoleId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "RoleId must be greater than 0",
                    errors = new { roleId }
                });
            }

            if (isActive != 0 && isActive != 1)
            {
                _log.Warn("Invalid IsActive value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 or 1",
                    errors = new { isActive }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _adminRepository.UpdateRoleMasterStatus(roleId, isActive, globalValues);

            if (serviceResult.Result)
                _log.Info($"Role status updated successfully: {serviceResult.Message}");
            else
                _log.Warn($"Role status update failed: {serviceResult.Message}");

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
        public IActionResult RoleMasterList([FromQuery] int? roleId = null)
        {
            _log.Info($"RoleMasterList called. RoleId={roleId?.ToString() ?? "All"}");

            var serviceResult = _adminRepository.RoleMasterList(roleId);

            if (serviceResult.Result)
                _log.Info($"Roles fetched successfully from cache: {serviceResult.Message}");
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

        [HttpPatch("updateUserMasterStatus")]
        [Authorize]
        public IActionResult UpdateUserMasterStatus([FromQuery] int userId, [FromQuery] int isActive)
        {
            _log.Info($"UpdateUserMasterStatus called. UserId={userId}, IsActive={isActive}");

            if (userId <= 0)
            {
                _log.Warn("Invalid UserId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "UserId must be greater than 0",
                    errors = new { userId }
                });
            }

            if (isActive != 0 && isActive != 1)
            {
                _log.Warn("Invalid IsActive value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 or 1",
                    errors = new { isActive }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _adminRepository.UpdateUserMasterStatus(userId, isActive, globalValues);

            if (serviceResult.Result)
                _log.Info($"User status updated successfully: {serviceResult.Message}");
            else
                _log.Warn($"User status update failed: {serviceResult.Message}");

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
        public IActionResult UserMasterList([FromQuery] int? userId = null)
        {
            _log.Info($"UserMasterList called. UserId={userId?.ToString() ?? "All"}");

            var serviceResult = _adminRepository.UserMasterList(userId);

            if (serviceResult.Result)
                _log.Info($"Users fetched successfully from cache: {serviceResult.Message}");
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
            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
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

        [HttpPatch("updateUserDepartmentStatus")]
        [Authorize]
        public IActionResult UpdateUserDepartmentStatus([FromQuery] int id, [FromQuery] int isActive)
        {
            _log.Info($"UpdateUserDepartmentStatus called. Id={id}, IsActive={isActive}");

            if (id <= 0)
            {
                _log.Warn("Invalid Id provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Id must be greater than 0",
                    errors = new { id }
                });
            }

            if (isActive != 0 && isActive != 1)
            {
                _log.Warn("Invalid IsActive value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 or 1",
                    errors = new { isActive }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _adminRepository.UpdateUserDepartmentStatus(id, isActive, globalValues);

            if (serviceResult.Result)
                _log.Info($"Department status updated successfully: {serviceResult.Message}");
            else
                _log.Warn($"Department status update failed: {serviceResult.Message}");

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
        public IActionResult UserDepartmentList([FromQuery] int? id = null)
        {
            _log.Info($"UserDepartmentList called. Id={id?.ToString() ?? "All"}");

            var serviceResult = _adminRepository.UserDepartmentList(id);

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
            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
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

        //[HttpGet("userGroupList")]
        //[Authorize]
        //public IActionResult UserGroupList()
        //{
        //    _log.Info("UserGroupList called.");
        //    var serviceResult = _adminRepository.UserGroupList();
        //    if (serviceResult.Result)
        //        _log.Info($"Groups fetched successfully: {serviceResult.Message}");
        //    else
        //        _log.Warn($"No groups found: {serviceResult.Message}");
        //    return StatusCode(serviceResult.StatusCode, new
        //    {
        //        result = serviceResult.Result,
        //        messageType = serviceResult.MessageType,
        //        message = serviceResult.Message,
        //        data = serviceResult.Data
        //    });
        //}

        [HttpPatch("updateUserGroupStatus")]
        [Authorize]
        public IActionResult UpdateUserGroupStatus([FromQuery] int id, [FromQuery] int isActive)
        {
            _log.Info($"UpdateUserGroupStatus called. Id={id}, IsActive={isActive}");

            if (id <= 0)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Id must be greater than 0"
                });
            }

            if (isActive != 0 && isActive != 1)
            {
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsActive must be 0 or 1"
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _adminRepository.UpdateUserGroupStatus(id, isActive, globalValues);

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
        public IActionResult UserGroupList([FromQuery] int? id = null)
        {
            _log.Info($"UserGroupList called. Id={id?.ToString() ?? "All"}");
            var serviceResult = _adminRepository.UserGroupList(id);

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
            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
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
        public IActionResult UserGroupMembersList([FromQuery] int? groupId)
        {
            _log.Info("UserGroupMembersList called.");
            if (groupId == null || groupId <= 0)
            {
                _log.Warn("Invalid GroupId supplied.");
                return BadRequest(new
                {
                    result = false,
                    messageType = "ERROR",
                    message = "GroupId is mandatory and must be greater than 0.",
                    data = ""
                });
            }

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

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

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

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

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

            if (branchId <= 0 || typeId <= 0 || userId <= 0)
            {
                _log.Warn("Invalid parameters for GetAssignUserRightMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, typeId, userId) must be greater than 0",
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

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

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

            if (branchId <= 0 || typeId <= 0 || userId <= 0 )
            {
                _log.Warn("Invalid parameters for GetAssignDashBoardUserRight.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, typeId, userId) must be greater than 0",
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

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

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

        [HttpGet("getNavigationTabMaster")]
        [Authorize]
        public IActionResult GetNavigationTabMaster()
        {
            _log.Info("GetNavigationTabMaster endpoint called.");

            var serviceResult = _adminRepository.GetNavigationTabMaster();

            if (serviceResult.Result)
                _log.Info($"Navigation tabs fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No navigation tabs found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("createUpdateNavigationSubMenuMaster")]
        [Authorize]
        public IActionResult CreateUpdateNavigationSubMenuMaster([FromBody] NavigationSubMenuMasterRequest request)
        {
            _log.Info($"CreateUpdateNavigationSubMenuMaster called. TabId={request.TabId}, SubMenuName={request.SubMenuName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for navigation sub menu insert/update.");
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

            var serviceResult = _adminRepository.CreateUpdateNavigationSubMenuMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Navigation sub menu operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Navigation sub menu operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getNavigationSubMenuMaster")]
        [Authorize]
        public IActionResult GetNavigationSubMenuMaster()
        {
            _log.Info($"GetNavigationSubMenuMaster called");

            var serviceResult = _adminRepository.GetNavigationSubMenuMaster();

            if (serviceResult.Result)
                _log.Info($"Navigation sub menus fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Navigation sub menus fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }



        [HttpPost("saveUpdateRoleWiseMenuMapping")]
        [Authorize]
        public IActionResult SaveUpdateRoleWiseMenuMapping([FromBody] SaveRoleWiseMenuMappingRequest request)
        {
            _log.Info($"SaveUpdateRoleWiseMenuMapping called. BranchId={request.BranchId}, RoleId={request.RoleId}, IsFirst={request.IsFirst}, MenuMappings Count={request.MenuMappings.Count}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveUpdateRoleWiseMenuMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate IsFirst parameter
            if (request.IsFirst != 0 && request.IsFirst != 1)
            {
                _log.Warn($"Invalid IsFirst parameter: {request.IsFirst}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsFirst must be either 0 or 1",
                    errors = new { IsFirst = request.IsFirst }
                });
            }

            // Validate that all items (if any exist) have the same branchId and roleId as the parent request
            if (request.MenuMappings != null && request.MenuMappings.Count > 0)
            {
                bool isConsistent = request.MenuMappings.All(x =>
                    x.BranchId == request.BranchId &&
                    x.RoleId == request.RoleId);

                if (!isConsistent)
                {
                    _log.Warn("Inconsistent branchId or roleId in menu mapping list.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "All menu mapping items must have the same branchId and roleId as the request"
                    });
                }

                _log.Info($"Saving role-wise menu mapping for BranchId={request.BranchId}, RoleId={request.RoleId}, Count={request.MenuMappings.Count}");
            }
            else
            {
                if (request.IsFirst == 1)
                {
                    _log.Info($"Removing all menu mappings for BranchId={request.BranchId}, RoleId={request.RoleId}");
                }
                else
                {
                    _log.Info($"No menu mappings to save for BranchId={request.BranchId}, RoleId={request.RoleId}");
                }
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

            var serviceResult = _adminRepository.SaveUpdateRoleWiseMenuMapping(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Role-wise menu mapping saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"Role-wise menu mapping save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getRoleWiseMenuMapping")]
        [Authorize]
        public IActionResult GetRoleWiseMenuMapping(
          [FromQuery] int branchId,
          [FromQuery] int roleId)
        {
            _log.Info($"GetRoleWiseMenuMapping called. BranchId={branchId}, RoleId={roleId}");

            if (branchId <= 0 || roleId <= 0)
            {
                _log.Warn("Invalid parameters for GetRoleWiseMenuMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, roleId) must be greater than 0",
                    errors = new { branchId, roleId }
                });
            }

            var serviceResult = _adminRepository.GetRoleWiseMenuMapping(branchId, roleId);

            if (serviceResult.Result)
                _log.Info($"Role-wise menu mapping fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"Role-wise menu mapping fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }





        [HttpPost("saveUpdateUserMenuMaster")]
        [Authorize]
        public IActionResult SaveUpdateUserMenuMaster([FromBody] SaveUserMenuMasterRequest request)
        {
            _log.Info($"SaveUpdateUserMenuMaster called. TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, RoleId={request.RoleId}, IsFirst={request.IsFirst}, UserMenus Count={request.UserMenus.Count}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveUpdateUserMenuMaster.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate IsFirst parameter
            if (request.IsFirst != 0 && request.IsFirst != 1)
            {
                _log.Warn($"Invalid IsFirst parameter: {request.IsFirst}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsFirst must be either 0 or 1",
                    errors = new { IsFirst = request.IsFirst }
                });
            }

            // Validate that all items (if any exist) have the same typeId, userId, branchId, and roleId as the parent request
            if (request.UserMenus != null && request.UserMenus.Count > 0)
            {
                bool isConsistent = request.UserMenus.All(x =>
                    x.TypeId == request.TypeId &&
                    x.UserId == request.UserId &&
                    x.BranchId == request.BranchId &&
                    x.RoleId == request.RoleId);

                if (!isConsistent)
                {
                    _log.Warn("Inconsistent typeId, userId, branchId, or roleId in user menu list.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "All user menu items must have the same typeId, userId, branchId, and roleId as the request"
                    });
                }

                _log.Info($"Saving user menu for TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, RoleId={request.RoleId}, Count={request.UserMenus.Count}");
            }
            

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

            var serviceResult = _adminRepository.SaveUpdateUserMenuMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"User menu master saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"User menu master save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getUserWiseMenuMaster")]
        [Authorize]
        public IActionResult GetUserWiseMenuMaster(
        [FromQuery] int branchId,
        [FromQuery] int typeId,
        [FromQuery] int userId,
        [FromQuery] int roleId)
        {
            _log.Info($"GetUserWiseMenuMaster called. BranchId={branchId}, TypeId={typeId}, UserId={userId},RoleId={roleId}");

            if (branchId <= 0 || typeId <= 0 || userId <= 0)
            {
                _log.Warn("Invalid parameters for GetUserWiseMenuMaster.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, typeId, userId) must be greater than 0",
                    errors = new { branchId, typeId, userId , roleId }
                });
            }

            var serviceResult = _adminRepository.GetUserWiseMenuMaster(branchId, typeId, userId, roleId);

            if (serviceResult.Result)
                _log.Info($"User-wise menu (granted + remaining) fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"User-wise menu fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }


        [HttpPost("saveUpdateUserCorporateMapping")]
        [Authorize]
        public IActionResult SaveUpdateUserCorporateMapping([FromBody] SaveUserCorporateMappingRequest request)
        {
            _log.Info($"SaveUpdateUserCorporateMapping called. TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, IsFirst={request.IsFirst}, UserCorporates Count={request.UserCorporates.Count}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveUpdateUserCorporateMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate IsFirst parameter
            if (request.IsFirst != 0 && request.IsFirst != 1)
            {
                _log.Warn($"Invalid IsFirst parameter: {request.IsFirst}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsFirst must be either 0 or 1",
                    errors = new { IsFirst = request.IsFirst }
                });
            }

            // Validate that all items (if any exist) have the same typeId, userId, and branchId as the parent request
            if (request.UserCorporates != null && request.UserCorporates.Count > 0)
            {
                bool isConsistent = request.UserCorporates.All(x =>
                    x.TypeId == request.TypeId &&
                    x.UserId == request.UserId &&
                    x.BranchId == request.BranchId);

                if (!isConsistent)
                {
                    _log.Warn("Inconsistent typeId, userId, or branchId in user corporate mapping list.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "All user corporate mapping items must have the same typeId, userId, and branchId as the request"
                    });
                }

                _log.Info($"Saving user corporate mapping for TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, Count={request.UserCorporates.Count}");
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

            var serviceResult = _adminRepository.SaveUpdateUserCorporateMapping(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"User corporate mapping saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"User corporate mapping save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getUserWiseCorporateMapping")]
        [Authorize]
        public IActionResult GetUserWiseCorporateMapping(
            [FromQuery] int branchId,
            [FromQuery] int typeId,
            [FromQuery] int userId)
        {
            _log.Info($"GetUserWiseCorporateMapping called. BranchId={branchId}, TypeId={typeId}, UserId={userId}");

            if (branchId <= 0 || typeId <= 0 || userId <= 0)
            {
                _log.Warn("Invalid parameters for GetUserWiseCorporateMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, typeId, userId) must be greater than 0",
                    errors = new { branchId, typeId, userId }
                });
            }

            var serviceResult = _adminRepository.GetUserWiseCorporateMapping(branchId, typeId, userId);

            if (serviceResult.Result)
                _log.Info($"User corporate mapping (granted + remaining) fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"User corporate mapping fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("saveUpdateUserBedMapping")]
        [Authorize]
        public IActionResult SaveUpdateUserBedMapping([FromBody] SaveUserBedMappingRequest request)
        {
            _log.Info($"SaveUpdateUserBedMapping called. TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, IsFirst={request.IsFirst}, UserBeds Count={request.UserBeds.Count}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for SaveUpdateUserBedMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate IsFirst parameter
            if (request.IsFirst != 0 && request.IsFirst != 1)
            {
                _log.Warn($"Invalid IsFirst parameter: {request.IsFirst}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsFirst must be either 0 or 1",
                    errors = new { IsFirst = request.IsFirst }
                });
            }

            // Validate that all items (if any exist) have the same typeId, userId, and branchId as the parent request
            if (request.UserBeds != null && request.UserBeds.Count > 0)
            {
                bool isConsistent = request.UserBeds.All(x =>
                    x.TypeId == request.TypeId &&
                    x.UserId == request.UserId &&
                    x.BranchId == request.BranchId);

                if (!isConsistent)
                {
                    _log.Warn("Inconsistent typeId, userId, or branchId in user bed mapping list.");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    return BadRequest(new
                    {
                        result = false,
                        messageType = alert.Type,
                        message = "All user bed mapping items must have the same typeId, userId, and branchId as the request"
                    });
                }

                _log.Info($"Saving user bed mapping for TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, Count={request.UserBeds.Count}");
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);

            var serviceResult = _adminRepository.SaveUpdateUserBedMapping(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"User bed mapping saved successfully: {serviceResult.Message}");
            else
                _log.Warn($"User bed mapping save failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getUserWiseBedMapping")]
        [Authorize]
        public IActionResult GetUserWiseBedMapping(
            [FromQuery] int branchId,
            [FromQuery] int typeId,
            [FromQuery] int userId)
        {
            _log.Info($"GetUserWiseBedMapping called. BranchId={branchId}, TypeId={typeId}, UserId={userId}");

            if (branchId <= 0 || typeId <= 0 || userId <= 0)
            {
                _log.Warn("Invalid parameters for GetUserWiseBedMapping.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "All parameters (branchId, typeId, userId) must be greater than 0",
                    errors = new { branchId, typeId, userId }
                });
            }

            var serviceResult = _adminRepository.GetUserWiseBedMapping(branchId, typeId, userId);

            if (serviceResult.Result)
                _log.Info($"User bed mapping (granted + remaining) fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"User bed mapping fetch failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("createUpdateBranchMaster")]
        [Authorize]
        public IActionResult CreateUpdateBranchMaster([FromBody] BranchMasterRequest request)
        {
            _log.Info($"CreateUpdateBranchMaster called. BranchName={request.BranchName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for branch insert/update.");
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
            var serviceResult = _adminRepository.CreateUpdateBranchMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Branch operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Branch operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getBranchDetails")]
        [Authorize]
        public IActionResult GetBranchDetails([FromQuery] int? branchId = null)
        {
            _log.Info($"GetBranchDetails called. BranchId={branchId?.ToString() ?? "All"}");

            var serviceResult = _adminRepository.GetBranchDetails(branchId);

            if (serviceResult.Result)
                _log.Info($"Branches fetched successfully from cache: {serviceResult.Message}");
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



        [HttpPost("createUpdateStateMaster")]
        [Authorize]
        public IActionResult CreateUpdateStateMaster([FromBody] CreateUpdateStateMasterRequest request)
        {
            _log.Info($"CreateUpdateStateMaster called. StateId={request.StateId}, StateName={request.StateName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for state master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Additional validation for CountryId
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
            var serviceResult = _adminRepository.CreateUpdateStateMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"State master operation completed: {serviceResult.Message}");
            else
                _log.Warn($"State master operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = new { stateId = serviceResult.Data }
            });
        }

        [HttpPost("createUpdateDistrictMaster")]
        [Authorize]
        public IActionResult CreateUpdateDistrictMaster([FromBody] CreateUpdateDistrictMasterRequest request)
        {
            _log.Info($"CreateUpdateDistrictMaster called. DistrictId={request.DistrictId}, DistrictName={request.DistrictName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for district master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Additional validation for StateId and CountryId
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
            var serviceResult = _adminRepository.CreateUpdateDistrictMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"District master operation completed: {serviceResult.Message}");
            else
                _log.Warn($"District master operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = new { districtId = serviceResult.Data }
            });
        }

        [HttpPost("createUpdateCityMaster")]
        [Authorize]
        public IActionResult CreateUpdateCityMaster([FromBody] CreateUpdateCityMasterRequest request)
        {
            _log.Info($"CreateUpdateCityMaster called. CityId={request.CityId}, CityName={request.CityName}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for city master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Additional validation for DistrictId, StateId, and CountryId
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
            var serviceResult = _adminRepository.CreateUpdateCityMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"City master operation completed: {serviceResult.Message}");
            else
                _log.Warn($"City master operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = new { cityId = serviceResult.Data }
            });
        }


        [HttpPost("createUpdatePincodeMaster")]
        [Authorize]
        public IActionResult CreateUpdatePincodeMaster([FromBody] CreateUpdatePincodeMasterRequest request)
        {
            _log.Info($"CreateUpdatePincodeMaster called. PincodeId={request.PincodeId}, CityId={request.CityId}, Pincode={request.Pincode}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for pincode master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Additional validation for CityId
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

            // Validate Pincode is exactly 6 digits
            if (request.Pincode < 100000 || request.Pincode > 999999)
            {
                _log.Warn($"Invalid Pincode provided: {request.Pincode}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Pincode must be exactly 6 digits",
                    errors = new { pincode = request.Pincode }
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
            var serviceResult = _adminRepository.CreateUpdatePincodeMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Pincode master operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Pincode master operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = new { pincodeId = serviceResult.Data }
            });
        }

        [HttpPost("createUpdateHeaderMaster")]
        [Authorize]
        public IActionResult CreateUpdateHeaderMaster([FromBody] HeaderMasterRequest request)
        {
            _log.Info($"CreateUpdateHeaderMaster called. HeaderId={request.HeaderId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for header insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate IsHeader value
            if (request.IsHeader != 0 && request.IsHeader != 1)
            {
                _log.Warn("Invalid IsHeader value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsHeader must be 0 or 1",
                    errors = new { isHeader = request.IsHeader }
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
            var serviceResult = _adminRepository.CreateUpdateHeaderMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Header operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Header operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getHeaderMaster")]
        [Authorize]
        public IActionResult GetHeaderMaster(
            [FromQuery] int branchId,
            [FromQuery] int roleId,
            [FromQuery] int typeId,
            [FromQuery] int isHeader)
        {
            _log.Info($"GetHeaderMaster called. BranchId={branchId}, RoleId={roleId}, TypeId={typeId}, IsHeader={isHeader}");

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

            if (roleId < 0)
            {
                _log.Warn("Invalid RoleId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "RoleId must be greater than Equal to 0",
                    errors = new { roleId }
                });
            }

            if (typeId <= 0)
            {
                _log.Warn("Invalid TypeId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "TypeId must be greater than 0",
                    errors = new { typeId }
                });
            }

            if (isHeader != 0 && isHeader != 1)
            {
                _log.Warn("Invalid IsHeader value provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "IsHeader must be 0 or 1",
                    errors = new { isHeader }
                });
            }

            var serviceResult = _adminRepository.GetHeaderMaster(branchId, roleId, typeId, isHeader);

            if (serviceResult.Result)
                _log.Info($"Headers fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No headers found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

       

        [HttpGet("getSequenceTypeList")]
        [Authorize]
        public IActionResult GetSequenceTypeList()
        {
            _log.Info("GetSequenceTypeList called.");

            var serviceResult = _adminRepository.GetSequenceTypeList();

            if (serviceResult.Result)
                _log.Info($"Sequence types fetched successfully: {serviceResult.Message}");
            else
                _log.Warn($"No sequence types found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPost("createUpdateSequenceMaster")]
        [Authorize]
        public IActionResult CreateUpdateSequenceMaster([FromBody] CreateUpdateSequenceMasterRequest request)
        {
            _log.Info($"CreateUpdateSequenceMaster called. SequenceId={request.SequenceId}, Name={request.Name}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for sequence master insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate TypeId (must be greater than 0)
            if (request.TypeId <= 0)
            {
                _log.Warn("Invalid TypeId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "TypeId must be greater than 0",
                    errors = new { typeId = request.TypeId }
                });
            }

            // Length validation is handled by [Range] attribute in the DTO
            // No need for additional validation here since ModelState will catch it

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _adminRepository.CreateUpdateSequenceMaster(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Sequence master operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Sequence master operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getSequenceMaster")]
        [Authorize]
        public IActionResult GetSequenceMaster([FromQuery] int sequenceTypeId)
        {
            _log.Info($"GetSequenceMaster called. SequenceTypeId={sequenceTypeId}");

            // Validate sequenceTypeId
            if (sequenceTypeId <= 0)
            {
                _log.Warn("Invalid SequenceTypeId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "SequenceTypeId must be greater than 0",
                    errors = new { sequenceTypeId }
                });
            }

            var serviceResult = _adminRepository.GetSequenceMaster(sequenceTypeId);

            if (serviceResult.Result)
                _log.Info($"Sequences fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No sequences found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

      

        [HttpPost("createUpdateBranchSequenceMapping")]
        [Authorize]
        public IActionResult CreateUpdateBranchSequenceMapping([FromBody] CreateUpdateBranchSequenceMappingRequest request)
        {
            _log.Info($"CreateUpdateBranchSequenceMapping called. MappingId={request.MappingId}, BranchId={request.BranchId}, RoleId={request.RoleId}, TypeId={request.TypeId}, SequenceId={request.SequenceId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for branch sequence mapping insert/update.");
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
            var serviceResult = _adminRepository.CreateUpdateBranchSequenceMapping(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Branch sequence mapping operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Branch sequence mapping operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getBranchSequenceMapping")]
        [Authorize]
        public IActionResult GetBranchSequenceMapping()
        {
            _log.Info("GetBranchSequenceMapping called.");

            var serviceResult = _adminRepository.GetBranchSequenceMapping();

            if (serviceResult.Result)
                _log.Info($"Branch sequence mappings fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No branch sequence mappings found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }



        [HttpPost("createUpdateLabReportLetterHead")]
        [Authorize]
        public IActionResult CreateUpdateLabReportLetterHead([FromForm] LabReportLetterHeadRequest request)
        {
            _log.Info($"CreateUpdateLabReportLetterHead called. Id={request.Id}, BranchId={request.BranchId}, TypeId={request.TypeId}");

            if (!ModelState.IsValid)
            {
                _log.Warn("Invalid model state for lab report letter head insert/update.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("MODEL_VALIDATION_FAILED");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = alert.Message,
                    errors = ModelState
                });
            }

            // Validate BranchId
            if (request.BranchId < 0)
            {
                _log.Warn("Invalid BranchId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "BranchId must be greater than 0",
                    errors = new { branchId = request.BranchId }
                });
            }

            // Validate TypeId
            if (request.TypeId < 0)
            {
                _log.Warn("Invalid TypeId provided.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "TypeId must be greater than 0",
                    errors = new { typeId = request.TypeId }
                });
            }

            // Validate file upload for new records (Id = 0)
            if (request.Id == 0 && (request.LetterHeadFile == null || request.LetterHeadFile.Length == 0))
            {
                _log.Warn("Letter head file is required for new records.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Letter head file is required for new letter head configuration",
                    errors = new { letterHeadFile = "Required" }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _adminRepository.CreateUpdateLabReportLetterHead(request, globalValues);

            if (serviceResult.Result)
                _log.Info($"Lab report letter head operation completed: {serviceResult.Message}");
            else
                _log.Warn($"Lab report letter head operation failed: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpGet("getLabReportLetterHeadList")]
        [Authorize]
        public IActionResult GetLabReportLetterHeadList()
        {
            _log.Info("GetLabReportLetterHeadList called.");

            var serviceResult = _adminRepository.GetLabReportLetterHeadList();

            if (serviceResult.Result)
                _log.Info($"Lab report letter heads fetched successfully from cache: {serviceResult.Message}");
            else
                _log.Warn($"No lab report letter heads found: {serviceResult.Message}");

            return StatusCode(serviceResult.StatusCode, new
            {
                result = serviceResult.Result,
                messageType = serviceResult.MessageType,
                message = serviceResult.Message,
                data = serviceResult.Data
            });
        }

        [HttpPatch("deleteLetterHeadMaster")]
        [Authorize]
        public IActionResult DeleteLetterHeadMaster([FromQuery] int id)
        {
            _log.Info($"DeleteLetterHeadMaster API called. Id={id}");

            if (id <= 0)
            {
                _log.Warn("Invalid Id provided for letter head deletion.");
                var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                return BadRequest(new
                {
                    result = false,
                    messageType = alert.Type,
                    message = "Id must be greater than 0",
                    errors = new { id }
                });
            }

            var globalValues = GlobalFunctions.GetGlobalValues(HttpContext);
            var serviceResult = _adminRepository.DeleteLetterHeadMaster(id, globalValues);

            if (serviceResult.Result)
                _log.Info($"Letter head deleted successfully: {serviceResult.Message}");
            else
                _log.Warn($"Letter head deletion failed: {serviceResult.Message}");

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
