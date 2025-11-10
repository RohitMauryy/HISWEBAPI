using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;
using HISWEBAPI.Services;
using Microsoft.Extensions.Logging;
using HISWEBAPI.Exceptions;
using System.Reflection;
using log4net;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Utilities;

namespace HISWEBAPI.Repositories.Implementations
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AdminRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
        }

        public ServiceResult<string> CreateUpdateRoleMaster(RoleMasterRequest request, AllGlobalValues globalValues)
        {
            try
            {

                var result = _sqlHelper.DML("IU_RoleMaster", CommandType.StoredProcedure, new
                {
                    @hospId = globalValues.hospId,
                    @roleId = request.RoleId,
                    @roleName = request.RoleName,
                    @isActive = request.IsActive,
                    @faIconId = request.FaIconId,
                    @userId = globalValues.userId,
                    @IpAddress = globalValues.ipAddress
                },
                new
                {
                    result = 0
                });

                if (result < 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        409 // Conflict
                    );
                }

                if (request.RoleId == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Role created successfully",
                        alert.Type,
                        alert.Message,
                        201 // Created
                    );
                }
                else
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Role updated successfully",
                        alert.Type,
                        alert.Message,
                        200 // OK
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<RoleMasterModel>> RoleMasterList()
        {
            try
            {

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetRoleList",
                    CommandType.StoredProcedure,
                    new { @roleName = "" }
                );

                var roles = dataTable?.AsEnumerable().Select(row => new RoleMasterModel
                {
                    RoleId = row.Field<int>("RoleId"),
                    RoleName = row.Field<string>("RoleName"),
                    FaIconId = row.Field<int>("FaIconId"),
                    IsActive = row.Field<int>("IsActive"),
                    IconName = row.Field<string>("IconName"),
                    IconClass = row.Field<string>("IconClass"),
                    CreatedBy = row.Field<string>("CreatedBy"),
                    CreatedOn = row.Field<string>("CreatedOn"),
                    LastModifiedBy = row.Field<string>("LastModifiedBy"),
                    LastModifiedOn = row.Field<string>("LastModifiedOn"),

                }).ToList() ?? new List<RoleMasterModel>();

                if (!roles.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<RoleMasterModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404 // Not Found
                    );
                }

                return ServiceResult<IEnumerable<RoleMasterModel>>.Success(
                    roles,
                    "Info",
                    $"{roles.Count} roles fetched successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<RoleMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<IEnumerable<FaIconModel>> getFaIconMaster()
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_getFaIconMaster",
                    CommandType.StoredProcedure
                );

                var dt = dataTable?.AsEnumerable().Select(row => new FaIconModel
                {
                    Id = row.Field<int>("Id"),
                    IconName = row.Field<string>("IconName"),
                    IconClass = row.Field<string>("IconClass"),

                }).ToList() ?? new List<FaIconModel>();

                if (!dt.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<FaIconModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404 // Not Found
                    );
                }
                var alert2 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");

                return ServiceResult<IEnumerable<FaIconModel>>.Success(
                    dt,
                     alert2.Type,
                     alert2.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<FaIconModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<UserMasterResponse> CreateUpdateUserMaster(UserMasterRequest request)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@HospId",1),
                    new SqlParameter("@Address", request.Address ?? (object)DBNull.Value),
                    new SqlParameter("@Contact", request.Contact ?? (object)DBNull.Value),
                    new SqlParameter("@DOB", request.DOB != default(DateTime) ? (object)request.DOB : DBNull.Value),
                    new SqlParameter("@Email", request.Email ?? (object)DBNull.Value),
                    new SqlParameter("@FirstName", request.FirstName),
                    new SqlParameter("@MidelName", request.MiddleName ?? (object)DBNull.Value),
                    new SqlParameter("@LastName", request.LastName ?? (object)DBNull.Value),
                    new SqlParameter("@Password", PasswordHasher.HashPassword(request.Password)),
                    new SqlParameter("@UserName", request.UserName),
                    new SqlParameter("@Gender", request.Gender ?? (object)DBNull.Value),
                    new SqlParameter("@UserId",request.userId),
                    new SqlParameter("@IsActive",request.IsActive),
                    new SqlParameter("@EmployeeID",request.EmployeeID),
                    new SqlParameter("@UserDepartmentId",request.UserDepartmentId),
                    new SqlParameter("@ReportToUserId",request.ReportToUserId),
                    new SqlParameter("@Result", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
                };

                long result = _sqlHelper.RunProcedureInsert("IU_UserMaster", parameters);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("USERNAME_EXISTS");
                    _log.Warn($"Duplicate username attempted: {request.UserName}");
                    return ServiceResult<UserMasterResponse>.Failure(
                        alert.Type,
                        alert.Message,
                        409
                    );
                }



                if (request.userId == 0)
                {
                    var responseData = new UserMasterResponse { userId = result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("USER_CREATED");
                    _log.Info($"User inserted successfully. UserId={result}");
                    return ServiceResult<UserMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        201
                    );
                }
                if (request.userId > 0)
                {
                    var responseData = new UserMasterResponse { userId = result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    _log.Info($"User Updated successfully. UserId={result}");
                    return ServiceResult<UserMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        201
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("USER_SAVE_FAILED");
                _log.Error("Failed to insert user. Result=0");
                return ServiceResult<UserMasterResponse>.Failure(
                    alert1.Type,
                    alert1.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SIGNUP_ERROR");
                return ServiceResult<UserMasterResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<IEnumerable<UserMasterModel>> UserMasterList()
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetUserMasterList",
                    CommandType.StoredProcedure,
                    new { }
                );

                var users = dataTable?.AsEnumerable().Select(row => new UserMasterModel
                {
                    Id = row.Field<int>("Id"),
                    FirstName = row.Field<string>("FirstName"),
                    MidelName = row.Field<string>("MidelName"),
                    LastName = row.Field<string>("LastName"),
                    DOB = row.Field<string>("DOB"),
                    Gender = row.Field<string>("Gender"),
                    UserName = row.Field<string>("UserName") ?? string.Empty,
                    Password = row.Field<string>("Password"),
                    Address = row.Field<string>("Address"),
                    Contact = row.Field<string>("Contact"),
                    Email = row.Field<string>("Email"),
                    IsActive = row.Field<int>("IsActive"),
                    EmployeeID = row.Field<string>("EmployeeID"),
                    CreatedBy = row.Field<string>("CreatedBy"),
                    CreatedOn = row.Field<string>("CreatedOn"),
                    LastModifiedBy = row.Field<string>("LastModifiedBy"),
                    LastModifiedOn = row.Field<string>("LastModifiedOn"),
                    ReportToUserId = row.Field<int?>("ReportToUserId"),
                    UserDepartmentId = row.Field<int?>("UserDepartmentId")
                }).ToList() ?? new List<UserMasterModel>();

                if (!users.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<UserMasterModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404 // Not Found
                    );
                }

                return ServiceResult<IEnumerable<UserMasterModel>>.Success(
                    users,
                    "Info",
                    $"{users.Count} users fetched successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<string> CreateUpdateUserDepartment(UserDepartmentRequest request, AllGlobalValues globalValues)
        {
            try
            {
                var result = _sqlHelper.DML("IU_UserDepartmentMaster", CommandType.StoredProcedure, new
                {
                    @Id = request.Id,
                    @DepartmentName = request.DepartmentName,
                    @IsActive = request.IsActive,
                    @userId = globalValues.userId,
                    @IpAddress = globalValues.ipAddress
                },
                new
                {
                    result = 0
                });

                if (result < 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        409 // Conflict
                    );
                }

                if (request.Id == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Department created successfully",
                        alert.Type,
                        alert.Message,
                        201 // Created
                    );
                }
                else
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Department updated successfully",
                        alert.Type,
                        alert.Message,
                        200 // OK
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<UserDepartmentMasterModel>> UserDepartmentList()
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetUserDepartmentList",
                    CommandType.StoredProcedure,
                    new { }
                );

                var departments = dataTable?.AsEnumerable().Select(row => new UserDepartmentMasterModel
                {
                    Id = row.Field<int>("Id"),
                    DepartmentName = row.Field<string>("DepartmentName") ?? string.Empty,
                    IsActive = row.Field<int>("IsActive"),
                    CreatedBy = row.Field<string>("CreatedBy"),
                    CreatedOn = row.Field<string>("CreatedOn"),
                    LastModifiedBy = row.Field<string>("LastModifiedBy"),
                    LastModifiedOn = row.Field<string>("LastModifiedOn"),
                    IPAddress = row.Field<string>("IPAddress")
                }).ToList() ?? new List<UserDepartmentMasterModel>();

                if (!departments.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<UserDepartmentMasterModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404 // Not Found
                    );
                }

                return ServiceResult<IEnumerable<UserDepartmentMasterModel>>.Success(
                    departments,
                    "Info",
                    $"{departments.Count} departments fetched successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserDepartmentMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<string> CreateUpdateUserGroupMaster(UserGroupRequest request, AllGlobalValues globalValues)
        {
            try
            {
                var result = _sqlHelper.DML("IU_UserGroupMaster", CommandType.StoredProcedure, new
                {
                    @Id = request.Id,
                    @GroupName = request.GroupName,
                    @IsActive = request.IsActive,
                    @userId = globalValues.userId,
                    @IpAddress = globalValues.ipAddress
                },
                new
                {
                    result = 0
                });

                if (result < 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        409 // Conflict
                    );
                }

                if (request.Id == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Group created successfully",
                        alert.Type,
                        alert.Message,
                        201 // Created
                    );
                }
                else
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Group updated successfully",
                        alert.Type,
                        alert.Message,
                        200 // OK
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<UserGroupMasterModel>> UserGroupList()
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetUserGroupList",
                    CommandType.StoredProcedure,
                    new { }
                );

                var groups = dataTable?.AsEnumerable().Select(row => new UserGroupMasterModel
                {
                    Id = row.Field<int>("Id"),
                    GroupName = row.Field<string>("GroupName") ?? string.Empty,
                    IsActive = row.Field<int>("IsActive"),
                    CreatedBy = row.Field<string>("CreatedBy"),
                    CreatedOn = row.Field<string>("CreatedOn"),
                    LastModifiedBy = row.Field<string>("LastModifiedBy"),
                    LastModifiedOn = row.Field<string>("LastModifiedOn"),
                    IPAddress = row.Field<string>("IPAddress")
                }).ToList() ?? new List<UserGroupMasterModel>();

                if (!groups.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<UserGroupMasterModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404 // Not Found
                    );
                }

                return ServiceResult<IEnumerable<UserGroupMasterModel>>.Success(
                    groups,
                    "Info",
                    $"{groups.Count} groups fetched successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserGroupMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<string> CreateUpdateUserGroupMembers(UserGroupMembersRequest request, AllGlobalValues globalValues)
        {
            try
            {
                string userIdsJson = System.Text.Json.JsonSerializer.Serialize(request.UserIds);

                var result = _sqlHelper.ExecuteScalar(
                    "IU_UserGroupMembers",
                    CommandType.StoredProcedure,
                    new
                    {
                        @GroupId = request.GroupId,
                        @UserIds = userIdsJson,
                        @userId = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
                    }
                );

                int rowCount = Convert.ToInt32(result);

                if (rowCount < 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                var alert2 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    $"{rowCount} group member(s) saved successfully",
                    alert2.Type,
                    alert2.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }
        public ServiceResult<IEnumerable<UserGroupMembersModel>> UserGroupMembersList(int? groupId)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetUserGroupMembersList",
                    CommandType.StoredProcedure,
                    new { @GroupId = groupId }
                );

                var members = dataTable?.AsEnumerable().Select(row => new UserGroupMembersModel
                {
                    Id = row.Field<int>("Id"),
                    GroupId = row.Field<int>("GroupId"),
                    UserId = row.Field<int>("UserId"),
                    GroupName = row.Field<string>("GroupName"),
                    UserName = row.Field<string>("UserName"),
                    IsActive = row.Field<int>("IsActive"),
                    CreatedBy = row.Field<string>("CreatedBy"),
                    CreatedOn = row.Field<string>("CreatedOn"),
                    LastModifiedBy = row.Field<string>("LastModifiedBy"),
                    LastModifiedOn = row.Field<string>("LastModifiedOn"),
                    IPAddress = row.Field<string>("IPAddress")
                }).ToList() ?? new List<UserGroupMembersModel>();

                if (!members.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<UserGroupMembersModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404 // Not Found
                    );
                }

                return ServiceResult<IEnumerable<UserGroupMembersModel>>.Success(
                    members,
                    "Info",
                    $"{members.Count} group member(s) fetched successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserGroupMembersModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }






        public ServiceResult<string> SaveUpdateRoleMapping(int userId, int branchId, int typeId, List<UserRoleMappingRequest> request, AllGlobalValues globalValues)
        {
            try
            {
                // Delete existing user role mappings
                var deleteResult = _sqlHelper.DML("D_DeleteUserRoleMapping", CommandType.StoredProcedure, new
                {
                    @UserId = userId,
                    @TypeId = typeId,
                    @BranchId = branchId
                },
                new
                {
                    result = 0
                });

                _log.Info($"Deleted existing role mappings for UserId={userId}, BranchId={branchId}, TypeId={typeId}");

                // If request list is empty or null, only delete operation is performed
                if (request == null || !request.Any())
                {
                    _log.Info($"No new roles to assign. All roles removed for UserId={userId}");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_DELETED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "All user roles removed successfully",
                        alert.Type,
                        alert.Message ?? "All roles removed successfully",
                        200
                    );
                }

                // Insert new role mappings
                int successCount = 0;
                foreach (var item in request)
                {
                    // Skip if roleId is 0
                    if (item.roleId == 0)
                    {
                        _log.Warn($"Skipping role assignment with RoleId=0 for UserId={item.userId}");
                        continue;
                    }

                    var result = _sqlHelper.DML("IU_UserRoleMapping", CommandType.StoredProcedure, new
                    {
                        @hospId = globalValues.hospId,
                        @TypeId = item.typeId,
                        @UserId = item.userId,
                        @BranchId = item.branchId,
                        @RoleId = item.roleId,
                        @IpAddress = globalValues.ipAddress,
                        @CreatedBy = globalValues.userId
                    },
                    new
                    {
                        result = 0
                    });

                    if (result < 0)
                    {
                        _log.Error($"Failed to insert role mapping for RoleId={item.roleId}");
                    }
                    else
                    {
                        successCount++;
                    }
                }

                if (successCount > 0)
                {
                    _log.Info($"Successfully inserted {successCount} role mapping(s) for UserId={userId}");
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        $"{successCount} user role(s) assigned successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }
                else if (request.Count > 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVE_FAILED");
                    _log.Error($"Failed to insert any role mappings for UserId={userId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "Failed to assign any roles",
                        500
                    );
                }

                // This case shouldn't happen, but handle it anyway
                var alert1 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    "User roles updated successfully",
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<UserRoleMappingModel>> GetAssignRoleForUserAuthorization(int branchId, int typeId, int userId)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetAssignRoleForUserAuthorization",
                    CommandType.StoredProcedure,
                    new
                    {
                        @BranchId = branchId,
                        @TypeId = typeId,
                        @UserId = userId
                    }
                );

                var roles = dataTable?.AsEnumerable().Select(row => new UserRoleMappingModel
                {
                    isGranted = row.Field<int>("isGranted"),
                    RoleName = row.Field<string>("RoleName") ?? string.Empty,
                    RoleId = row.Field<int>("RoleId")
                }).ToList() ?? new List<UserRoleMappingModel>();

                if (!roles.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"No role authorization data found for UserId={userId}, BranchId={branchId}, TypeId={typeId}");
                    return ServiceResult<IEnumerable<UserRoleMappingModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {roles.Count} role authorization records for UserId={userId}");

                return ServiceResult<IEnumerable<UserRoleMappingModel>>.Success(
                    roles,
                    "Info",
                    $"{roles.Count} role(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserRoleMappingModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }



        public ServiceResult<string> SaveUpdateUserRightMapping(SaveUserRightMappingRequest request, AllGlobalValues globalValues)
        {
            try
            {
                // First, delete existing user right mappings for this user/branch/role/type combination
                var deleteResult = _sqlHelper.DML("D_DeleteUserRightMapping", CommandType.StoredProcedure, new
                {
                    @TypeId = request.TypeId,
                    @UserId = request.UserId,
                    @BranchId = request.BranchId,
                    @RoleId = request.RoleId
                },
                new
                {
                    result = 0
                });

                _log.Info($"Deleted existing user rights for UserId={request.UserId}, BranchId={request.BranchId}, RoleId={request.RoleId}, TypeId={request.TypeId}");

                // If UserRights list is empty or null, only delete operation was needed
                if (request.UserRights == null || !request.UserRights.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_DELETED_SUCCESSFULLY");
                    _log.Info("User rights deleted successfully. No new rights to insert.");

                    return ServiceResult<string>.Success(
                        "User rights deleted successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // If UserRights list has items with UserRightId = 0, it means no rights to assign
                var validUserRights = request.UserRights.Where(ur => ur.UserRightId != 0).ToList();

                if (!validUserRights.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_DELETED_SUCCESSFULLY");
                    _log.Info("User rights deleted successfully. No valid rights to insert.");

                    return ServiceResult<string>.Success(
                        "User rights deleted successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Insert new user right mappings
                int insertedCount = 0;
                foreach (var userRight in validUserRights)
                {
                    var result = _sqlHelper.DML("IU_UserRightMapping", CommandType.StoredProcedure, new
                    {
                        @hospId = globalValues.hospId,
                        @TypeId = userRight.TypeId,
                        @UserId = userRight.UserId,
                        @BranchId = userRight.BranchId,
                        @RoleId = userRight.RoleId,
                        @UserRightId = userRight.UserRightId,
                        @IpAddress = globalValues.ipAddress,
                        @CreatedBy = globalValues.userId
                    },
                    new
                    {
                        result = 0
                    });

                    if (result > 0)
                    {
                        insertedCount++;
                    }
                }

                _log.Info($"Inserted {insertedCount} user rights for UserId={request.UserId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    $"User rights updated successfully. {insertedCount} right(s) assigned.",
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<IEnumerable<UserRightMappingModel>> GetAssignUserRightMapping(
         int branchId,
         int typeId,
         int userId,
         int roleId)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_getAssignUserRightMapping",
                    CommandType.StoredProcedure,
                    new
                    {
                        @BranchId = branchId,
                        @typeId = typeId,
                        @UserId = userId,
                        @RoleId = roleId
                    }
                );

                var userRights = dataTable?.AsEnumerable().Select(row => new UserRightMappingModel
                {
                    IsGranted = row.Field<int>("isGranted"),
                    UserRightName = row.Field<string>("UserRightName") ?? string.Empty,
                    Description = row.Field<string>("Description") ?? string.Empty,
                    UserRightId = row.Field<int>("UserRightId")
                }).ToList() ?? new List<UserRightMappingModel>();

                if (!userRights.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No user rights found for BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

                    return ServiceResult<IEnumerable<UserRightMappingModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {userRights.Count} user rights mapping records");

                return ServiceResult<IEnumerable<UserRightMappingModel>>.Success(
                    userRights,
                    "Info",
                    $"{userRights.Count} user right(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserRightMappingModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }



        public ServiceResult<string> SaveUpdateDashBoardUserRightMapping(SaveDashboardUserRightMappingRequest request, AllGlobalValues globalValues)
        {
            try
            {
                // First, delete existing dashboard user right mappings for this user/branch/role/type combination
                var deleteResult = _sqlHelper.DML("D_DeleteDashBoardUserRightMapping", CommandType.StoredProcedure, new
                {
                    @TypeId = request.TypeId,
                    @UserId = request.UserId,
                    @BranchId = request.BranchId,
                    @RoleId = request.RoleId
                },
                new
                {
                    result = 0
                });

                _log.Info($"Deleted existing dashboard user rights for UserId={request.UserId}, BranchId={request.BranchId}, RoleId={request.RoleId}, TypeId={request.TypeId}");

                // If DashboardUserRights list is empty or null, only delete operation was needed
                if (request.DashboardUserRights == null || !request.DashboardUserRights.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_DELETED_SUCCESSFULLY");
                    _log.Info("Dashboard user rights deleted successfully. No new rights to insert.");

                    return ServiceResult<string>.Success(
                        "Dashboard user rights deleted successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // If DashboardUserRights list has items with UserRightId = 0, it means no rights to assign
                var validDashboardRights = request.DashboardUserRights.Where(ur => ur.UserRightId != 0).ToList();

                if (!validDashboardRights.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_DELETED_SUCCESSFULLY");
                    _log.Info("Dashboard user rights deleted successfully. No valid rights to insert.");

                    return ServiceResult<string>.Success(
                        "Dashboard user rights deleted successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Insert new dashboard user right mappings
                int insertedCount = 0;
                foreach (var dashboardRight in validDashboardRights)
                {
                    var result = _sqlHelper.DML("IU_DashBoardUserRightMapping", CommandType.StoredProcedure, new
                    {
                        @hospId = globalValues.hospId,
                        @TypeId = dashboardRight.TypeId,
                        @UserId = dashboardRight.UserId,
                        @BranchId = dashboardRight.BranchId,
                        @RoleId = dashboardRight.RoleId,
                        @UserRightId = dashboardRight.UserRightId,
                        @IpAddress = globalValues.ipAddress,
                        @CreatedBy = globalValues.userId
                    },
                    new
                    {
                        result = 0
                    });

                    if (result > 0)
                    {
                        insertedCount++;
                    }
                }

                _log.Info($"Inserted {insertedCount} dashboard user rights for UserId={request.UserId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    $"Dashboard user rights updated successfully. {insertedCount} dashboard right(s) assigned.",
                    alert1.Type,
                    alert1.Message,
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<string>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }



        public ServiceResult<IEnumerable<DashboardUserRightMappingModel>> GetAssignDashBoardUserRight(
            int branchId,
            int typeId,
            int userId,
            int roleId)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_getAssignDashBoardUserRight",
                    CommandType.StoredProcedure,
                    new
                    {
                        @BranchId = branchId,
                        @TypeId = typeId,
                        @UserId = userId,
                        @RoleId = roleId
                    }
                );

                var dashboardRights = dataTable?.AsEnumerable().Select(row => new DashboardUserRightMappingModel
                {
                    IsGranted = row.Field<int>("isGranted"),
                    UserRightName = row.Field<string>("UserRightName") ?? string.Empty,
                    Details = row.Field<string>("Details") ?? string.Empty,
                    UserRightId = row.Field<int>("UserRightId")
                }).ToList() ?? new List<DashboardUserRightMappingModel>();

                if (!dashboardRights.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No dashboard user rights found for BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

                    return ServiceResult<IEnumerable<DashboardUserRightMappingModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {dashboardRights.Count} dashboard user rights mapping records");

                return ServiceResult<IEnumerable<DashboardUserRightMappingModel>>.Success(
                    dashboardRights,
                    "Info",
                    $"{dashboardRights.Count} dashboard user right(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<DashboardUserRightMappingModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }




        public ServiceResult<NavigationTabMasterResponse> CreateUpdateNavigationTabMaster(NavigationTabMasterRequest request, AllGlobalValues globalValues)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@TabId", request.TabId),
            new SqlParameter("@HospId", globalValues.hospId),
            new SqlParameter("@TabName", request.TabName),
            new SqlParameter("@FaIconId", request.FaIconId),
            new SqlParameter("@IpAddress", globalValues.ipAddress),
            new SqlParameter("@CreatedOn", globalValues.userId),
            new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                int result = (int)_sqlHelper.RunProcedureInsert("IU_NavigationTabMaster", parameters);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate navigation tab name attempted: {request.TabName}");
                    return ServiceResult<NavigationTabMasterResponse>.Failure(
                        alert.Type,
                        alert.Message,
                        409
                    );
                }

                if (request.TabId == 0)
                {
                    var responseData = new NavigationTabMasterResponse { TabId = result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    _log.Info($"Navigation tab created successfully. TabId={result}");
                    return ServiceResult<NavigationTabMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        201
                    );
                }
                else
                {
                    var responseData = new NavigationTabMasterResponse { TabId = result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    _log.Info($"Navigation tab updated successfully. TabId={result}");
                    return ServiceResult<NavigationTabMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        200
                    );
                }
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<NavigationTabMasterResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


    }
}
