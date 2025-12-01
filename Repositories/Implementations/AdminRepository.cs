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
using Microsoft.Extensions.Caching.Distributed;

namespace HISWEBAPI.Repositories.Implementations
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;
        private readonly IDistributedCache _distributedCache;
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AdminRepository(
            ICustomSqlHelper sqlHelper,
            IResponseMessageService messageService,
            IDistributedCache distributedCache)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;
            _distributedCache = distributedCache;

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
                _distributedCache.Remove("_RoleMaster_All");
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

        public ServiceResult<string> UpdateRoleMasterStatus(int roleId, int isActive, AllGlobalValues globalValues)
        {
            try
            {
                var result = _sqlHelper.DML("U_UpdateRoleMasterStatus", CommandType.StoredProcedure, new
                {
                    @roleId = roleId,
                    @userId = globalValues.userId,
                    @isActive = isActive
                });
                _distributedCache.Remove("_RoleMaster_All");
                if (result > 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    _log.Info($"Role status updated successfully. RoleId={roleId}, IsActive={isActive}");
                    return ServiceResult<string>.Success(
                        "Role status updated successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }
                else
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"Role not found for RoleId={roleId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "Role not found",
                        404
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


        public ServiceResult<IEnumerable<RoleMasterModel>> RoleMasterList(int? roleId = null)
        {
            try
            {
                _log.Info($"RoleMasterList called. RoleId={roleId?.ToString() ?? "All"}");

                // Always use the same cache key for all roles
                string cacheKey = "_RoleMaster_All";

                // Try to get all roles from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<RoleMasterModel> allRoles;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"RoleMaster data retrieved from cache. Key={cacheKey}");
                    allRoles = System.Text.Json.JsonSerializer.Deserialize<List<RoleMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"RoleMaster cache miss. Fetching all data from database. Key={cacheKey}");

                    // Fetch ALL roles from database (NO parameters passed - SP returns everything)
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetRoleList",
                        CommandType.StoredProcedure
                    // No parameters - SP always returns all roles
                    );

                    allRoles = dataTable?.AsEnumerable().Select(row => new RoleMasterModel
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

                    // Store ALL roles in cache (no expiration)
                    if (allRoles.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allRoles);
                        var cacheOptions = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All RoleMaster data cached permanently. Key={cacheKey}, Count={allRoles.Count}");
                    }
                }

                // Filter in memory based on roleId parameter (always from cache)
                List<RoleMasterModel> filteredRoles;
                if (roleId.HasValue)
                {
                    _log.Info($"Filtering cached data by RoleId: {roleId.Value}");
                    filteredRoles = allRoles.Where(r => r.RoleId == roleId.Value).ToList();
                }
                else
                {
                    _log.Info("Returning all cached roles");
                    filteredRoles = allRoles;
                }

                if (!filteredRoles.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No roles found for RoleId: {roleId?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<RoleMasterModel>>.Failure(
                        alert.Type,
                        roleId.HasValue
                            ? $"Role not found for RoleId: {roleId.Value}"
                            : "No roles found",
                        404
                    );
                }

                _log.Info($"Retrieved {filteredRoles.Count} role(s) from cache");

                return ServiceResult<IEnumerable<RoleMasterModel>>.Success(
                    filteredRoles,
                    "Info",
                    $"{filteredRoles.Count} role(s) fetched successfully",
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
                _log.Info("getFaIconMaster called.");

                // Cache key for FaIcon
                string cacheKey = "_FaIconMaster_All";

                // Try to get cached data
                var cachedData = _distributedCache.GetString(cacheKey);
                List<FaIconModel> faIcons;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"FaIconMaster data retrieved from cache. Key={cacheKey}");

                    faIcons = System.Text.Json.JsonSerializer.Deserialize<List<FaIconModel>>(cachedData);
                }
                else
                {
                    _log.Info($"FaIconMaster cache miss. Fetching from database. Key={cacheKey}");

                    // Fetch from DB
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_getFaIconMaster",
                        CommandType.StoredProcedure
                    );

                    faIcons = dataTable?.AsEnumerable().Select(row => new FaIconModel
                    {
                        Id = row.Field<int>("Id"),
                        IconName = row.Field<string>("IconName"),
                        IconClass = row.Field<string>("IconClass")
                    }).ToList() ?? new List<FaIconModel>();

                    // Store in Redis permanently
                    if (faIcons.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(faIcons);

                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };

                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);

                        _log.Info($"FaIconMaster cached permanently. Key={cacheKey}, Count={faIcons.Count}");
                    }
                }

                // Check if empty
                if (!faIcons.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");

                    _log.Info("No FaIcon data found");
                    return ServiceResult<IEnumerable<FaIconModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                var alertSuccess = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");

                _log.Info($"Retrieved {faIcons.Count} fa icon(s) from cache");

                return ServiceResult<IEnumerable<FaIconModel>>.Success(
                    faIcons,
                    alertSuccess.Type,
                    $"{faIcons.Count} fa icon(s) retrieved successfully",
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
                _distributedCache.Remove("_UserMaster_All");

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

        public ServiceResult<string> UpdateUserMasterStatus(int userId, int isActive, AllGlobalValues globalValues)
        {
            try
            {
                var result = _sqlHelper.DML("U_UpdateUserMasterStatus", CommandType.StoredProcedure, new
                {
                    @userId = userId,
                    @loginUserId = globalValues.userId,
                    @isActive = isActive
                });
                _distributedCache.Remove("_UserMaster_All");

                if (result > 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    _log.Info($"User status updated successfully. UserId={userId}, IsActive={isActive}");
                    return ServiceResult<string>.Success(
                        "User status updated successfully",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }
                else
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"User not found for UserId={userId}");
                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "User not found",
                        404
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

        public ServiceResult<IEnumerable<UserMasterModel>> UserMasterList(int? userId = null)
        {
            try
            {
                _log.Info($"UserMasterList called. UserId={userId?.ToString() ?? "All"}");

                string cacheKey = "_UserMaster_All";

                // Try to get all roles from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserMasterModel> allUsers;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserMaster data retrieved from cache. Key={cacheKey}");
                    allUsers = System.Text.Json.JsonSerializer.Deserialize<List<UserMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"UserMaster cache miss. Fetching all data from database. Key={cacheKey}");

                    // Fetch ALL users from database (NO parameters - SP returns everything)
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetUserMasterList",
                        CommandType.StoredProcedure
                    // No parameters - SP always returns all users
                    );

                    allUsers = dataTable?.AsEnumerable().Select(row => new UserMasterModel
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

                    // Store ALL users in cache (no expiration)
                    if (allUsers.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allUsers);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All UserMaster data cached permanently. Key={cacheKey}, Count={allUsers.Count}");
                    }
                }

                // Filter in memory based on userId parameter (always from cache)
                List<UserMasterModel> filteredUsers;
                if (userId.HasValue)
                {
                    _log.Info($"Filtering cached data by UserId: {userId.Value}");
                    filteredUsers = allUsers.Where(u => u.Id == userId.Value).ToList();
                }
                else
                {
                    _log.Info("Returning all cached users");
                    filteredUsers = allUsers;
                }

                if (!filteredUsers.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No users found for UserId: {userId?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<UserMasterModel>>.Failure(
                        alert.Type,
                        userId.HasValue
                            ? $"User not found for UserId: {userId.Value}"
                            : "No users found",
                        404
                    );
                }

                _log.Info($"Retrieved {filteredUsers.Count} user(s) from cache");

                return ServiceResult<IEnumerable<UserMasterModel>>.Success(
                    filteredUsers,
                    "Info",
                    $"{filteredUsers.Count} user(s) fetched successfully",
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
                // Clear cache after successful operation
                _distributedCache.Remove("_UserDepartment_All");

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


        public ServiceResult<string> UpdateUserDepartmentStatus(int id, int isActive, AllGlobalValues globalValues)
        {
            try
            {
                var result = _sqlHelper.DML("U_UserDepartmentStatus", CommandType.StoredProcedure, new
                {
                    @Id = id,
                    @IsActive = isActive,
                    @UserId = globalValues.userId,
                    @IpAddress = globalValues.ipAddress
                });

                // Clear cache after successful update
                _distributedCache.Remove("_UserDepartment_All");

                var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                _log.Info($"User department status updated successfully. Id={id}, IsActive={isActive}");
                return ServiceResult<string>.Success(
                    "Department status updated successfully",
                    alert.Type,
                    alert.Message,
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

        public ServiceResult<IEnumerable<UserDepartmentMasterModel>> UserDepartmentList(int? id = null)
        {
            try
            {
                _log.Info($"UserDepartmentList called. Id={id?.ToString() ?? "All"}");

                string cacheKey = "_UserDepartment_All";

                // Try to get all departments from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserDepartmentMasterModel> allDepartments;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserDepartment data retrieved from cache. Key={cacheKey}");
                    allDepartments = System.Text.Json.JsonSerializer.Deserialize<List<UserDepartmentMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"UserDepartment cache miss. Fetching all data from database. Key={cacheKey}");

                    // Fetch ALL departments from database (NO parameters - SP returns everything)
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetUserDepartmentList",
                        CommandType.StoredProcedure
                    // No parameters - SP always returns all departments
                    );

                    allDepartments = dataTable?.AsEnumerable().Select(row => new UserDepartmentMasterModel
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

                    // Store ALL departments in cache (no expiration)
                    if (allDepartments.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allDepartments);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All UserDepartment data cached permanently. Key={cacheKey}, Count={allDepartments.Count}");
                    }
                }

                // Filter in memory based on id parameter (always from cache)
                List<UserDepartmentMasterModel> filteredDepartments;
                if (id.HasValue)
                {
                    _log.Info($"Filtering cached data by Id: {id.Value}");
                    filteredDepartments = allDepartments.Where(d => d.Id == id.Value).ToList();
                }
                else
                {
                    _log.Info("Returning all cached departments");
                    filteredDepartments = allDepartments;
                }

                if (!filteredDepartments.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No departments found for Id: {id?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<UserDepartmentMasterModel>>.Failure(
                        alert.Type,
                        id.HasValue
                            ? $"Department not found for Id: {id.Value}"
                            : "No departments found",
                        404
                    );
                }

                _log.Info($"Retrieved {filteredDepartments.Count} department(s) from cache");

                return ServiceResult<IEnumerable<UserDepartmentMasterModel>>.Success(
                    filteredDepartments,
                    "Info",
                    $"{filteredDepartments.Count} department(s) fetched successfully",
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
                _distributedCache.Remove("_UserGroupMaster_All");

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

        //public ServiceResult<IEnumerable<UserGroupMasterModel>> UserGroupList()
        //{
        //    try
        //    {
        //        var dataTable = _sqlHelper.GetDataTable(
        //            "S_GetUserGroupList",
        //            CommandType.StoredProcedure,
        //            new { }
        //        );

        //        var groups = dataTable?.AsEnumerable().Select(row => new UserGroupMasterModel
        //        {
        //            Id = row.Field<int>("Id"),
        //            GroupName = row.Field<string>("GroupName") ?? string.Empty,
        //            IsActive = row.Field<int>("IsActive"),
        //            CreatedBy = row.Field<string>("CreatedBy"),
        //            CreatedOn = row.Field<string>("CreatedOn"),
        //            LastModifiedBy = row.Field<string>("LastModifiedBy"),
        //            LastModifiedOn = row.Field<string>("LastModifiedOn"),
        //            IPAddress = row.Field<string>("IPAddress")
        //        }).ToList() ?? new List<UserGroupMasterModel>();

        //        if (!groups.Any())
        //        {
        //            var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
        //            return ServiceResult<IEnumerable<UserGroupMasterModel>>.Failure(
        //                alert.Type,
        //                alert.Message,
        //                404 // Not Found
        //            );
        //        }

        //        return ServiceResult<IEnumerable<UserGroupMasterModel>>.Success(
        //            groups,
        //            "Info",
        //            $"{groups.Count} groups fetched successfully",
        //            200
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
        //        var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
        //        return ServiceResult<IEnumerable<UserGroupMasterModel>>.Failure(
        //            alert.Type,
        //            alert.Message,
        //            500
        //        );
        //    }
        //}

        public ServiceResult<string> UpdateUserGroupStatus(int id, int isActive, AllGlobalValues globalValues)
        {
            try
            {
                var result = _sqlHelper.DML("U_UpdateUserGroupStatus", CommandType.StoredProcedure, new
                {
                    @Id = id,
                    @IsActive = isActive,
                    @UserId = globalValues.userId,
                    @IpAddress = globalValues.ipAddress
                });

                _distributedCache.Remove("_UserGroupMaster_All");

                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    return ServiceResult<string>.Success(
                        "Group status updated successfully",
                        alert.Type,
                        alert.Message,
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

        public ServiceResult<IEnumerable<UserGroupMasterModel>> UserGroupList(int? id = null)
        {
            try
            {
                _log.Info($"UserGroupList called. Id={id?.ToString() ?? "All"}");

                string cacheKey = "_UserGroupMaster_All";

                // Try from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserGroupMasterModel> allGroups;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info("UserGroupMaster data retrieved from cache");
                    allGroups = System.Text.Json.JsonSerializer.Deserialize<List<UserGroupMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info("UserGroupMaster cache miss. Fetching from DB");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetUserGroupList",
                        CommandType.StoredProcedure
                    );

                    allGroups = dataTable?.AsEnumerable().Select(row => new UserGroupMasterModel
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

                    if (allGroups.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allGroups);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info("UserGroupMaster cached permanently");
                    }
                }

                // Filter
                List<UserGroupMasterModel> filteredGroups;
                if (id.HasValue)
                    filteredGroups = allGroups.Where(x => x.Id == id.Value).ToList();
                else
                    filteredGroups = allGroups;

                if (!filteredGroups.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    return ServiceResult<IEnumerable<UserGroupMasterModel>>.Failure(
                        alert.Type,
                        id.HasValue ? "Group not found" : "No groups found",
                        404
                    );
                }

                return ServiceResult<IEnumerable<UserGroupMasterModel>>.Success(
                    filteredGroups,
                    "Info",
                    $"{filteredGroups.Count} record(s) fetched successfully",
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
                    isGranted = row.Field<int>("isGranted"),
                    GroupId = row.Field<int>("GroupId"),
                    UserId = row.Field<int>("UserId"),
                    GroupName = row.Field<string>("GroupName"),
                    UserName = row.Field<string>("UserName")
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


        public ServiceResult<IEnumerable<NavigationTabMasterModel>> GetNavigationTabMaster()
        {
            try
            {
                _log.Info("GetNavigationTabMaster called.");

                // Define cache key inside the method
                string cacheKey = "_NavigationTabMaster_All";

                // Try to get data from Redis cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<NavigationTabMasterModel> navigationTabs;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"NavigationTabMaster data retrieved from cache. Key={cacheKey}");
                    navigationTabs = System.Text.Json.JsonSerializer.Deserialize<List<NavigationTabMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"NavigationTabMaster cache miss. Fetching data from database. Key={cacheKey}");

                    // Fetch data from database using stored procedure
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetNavigationTabMaster",
                        CommandType.StoredProcedure
                    );

                    navigationTabs = dataTable?.AsEnumerable().Select(row => new NavigationTabMasterModel
                    {
                        TabId = row.Field<int>("TabId"),
                        TabName = row.Field<string>("TabName") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<NavigationTabMasterModel>();

                    // Store data in Redis cache with unlimited time span (no expiration)
                    if (navigationTabs.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(navigationTabs);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"NavigationTabMaster data cached permanently. Key={cacheKey}, Count={navigationTabs.Count}");
                    }
                }

                if (!navigationTabs.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info("No navigation tabs found");
                    return ServiceResult<IEnumerable<NavigationTabMasterModel>>.Failure(
                        alert.Type,
                        "No navigation tabs found",
                        404
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_COMPLETED_SUCCESSFULLY");
                _log.Info($"Retrieved {navigationTabs.Count} navigation tab(s) from cache");

                return ServiceResult<IEnumerable<NavigationTabMasterModel>>.Success(
                    navigationTabs,
                    alert1.Type,
                    $"{navigationTabs.Count} navigation tab(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<NavigationTabMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<NavigationSubMenuMasterResponse> CreateUpdateNavigationSubMenuMaster(
            NavigationSubMenuMasterRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@SubMenuId", request.SubMenuId),
            new SqlParameter("@TabId", request.TabId),
            new SqlParameter("@SubMenuName", request.SubMenuName),
            new SqlParameter("@HospId", globalValues.hospId),
            new SqlParameter("@URL", request.URL ?? (object)DBNull.Value),
            new SqlParameter("@IpAddress", globalValues.ipAddress),
            new SqlParameter("@CreatedOn", globalValues.userId),
            new SqlParameter("@IsActive", request.IsActive ? 1 : 0),
            new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                int result = (int)_sqlHelper.RunProcedureInsert("IU_NavigationSubMenuMaster", parameters);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate navigation sub menu name attempted: {request.SubMenuName}");
                    return ServiceResult<NavigationSubMenuMasterResponse>.Failure(
                        alert.Type,
                        $"Sub menu '{request.SubMenuName}' already exists",
                        409
                    );
                }

                if (request.SubMenuId == 0)
                {
                    var responseData = new NavigationSubMenuMasterResponse { SubMenuId = result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    _log.Info($"Navigation sub menu created successfully. SubMenuId={result}");
                    return ServiceResult<NavigationSubMenuMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        201
                    );
                }
                else
                {
                    var responseData = new NavigationSubMenuMasterResponse { SubMenuId = result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    _log.Info($"Navigation sub menu updated successfully. SubMenuId={result}");
                    return ServiceResult<NavigationSubMenuMasterResponse>.Success(
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
                return ServiceResult<NavigationSubMenuMasterResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<NavigationSubMenuMasterModel>> GetNavigationSubMenuMaster()
        {
            try
            {
                _log.Info($"GetNavigationSubMenuMaster called");

                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetNavigationSubMenuMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                       
                    }
                );

                var subMenus = dataTable?.AsEnumerable().Select(row => new NavigationSubMenuMasterModel
                {
                    SubMenuId = row.Field<int>("SubMenuId"),
                    TabId = row.Field<int>("TabId"),
                    SubMenuName = row.Field<string>("SubMenuName") ?? string.Empty,
                    URL = row.Field<string>("URL") ?? string.Empty,
                    IsActive = row.Field<int>("IsActive"),
                    CreatedBy = row.Field<string>("CreatedBy") ?? string.Empty,
                    CreatedOn = row.Field<string>("CreatedOn") ?? string.Empty,
                    LastModifiedBy = row.Field<string>("LastModifiedBy") ?? string.Empty,
                    LastModifiedOn = row.Field<string>("LastModifiedOn") ?? string.Empty,
                    IpAddress = row.Field<string>("IpAddress") ?? string.Empty
                }).ToList() ?? new List<NavigationSubMenuMasterModel>();

                if (!subMenus.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No navigation sub menus found");

                    return ServiceResult<IEnumerable<NavigationSubMenuMasterModel>>.Failure(
                        alert.Type,
                        "No navigation sub menus found",
                        404
                    );
                }

                _log.Info($"Retrieved {subMenus.Count} navigation sub menu(s)");

                return ServiceResult<IEnumerable<NavigationSubMenuMasterModel>>.Success(
                    subMenus,
                    "Info",
                    $"{subMenus.Count} navigation sub menu(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<NavigationSubMenuMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }





        public ServiceResult<string> SaveUpdateRoleWiseMenuMapping(
      SaveRoleWiseMenuMappingRequest request,
      AllGlobalValues globalValues)
        {
            try
            {
                // Delete existing role-wise menu mappings only if IsFirst = 1
                if (request.IsFirst == 1)
                {
                    var deleteResult = _sqlHelper.DML("D_DeleteRoleWiseMenuMappingMaster", CommandType.StoredProcedure, new
                    {
                        @BranchId = request.BranchId,
                        @RoleId = request.RoleId
                    },
                    new
                    {
                        result = 0
                    });

                    _log.Info($"Deleted existing role-wise menu mappings for BranchId={request.BranchId}, RoleId={request.RoleId}");
                }

                // If MenuMappings list is empty or null, only delete operation was needed
                if (request.MenuMappings == null || !request.MenuMappings.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("Role-wise menu mapping operation completed. No new mappings to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "Role-wise menu mappings deleted successfully" : "No menu mappings to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Filter out items with SubMenuId = 0
                var validMenuMappings = request.MenuMappings.Where(mm => mm.SubMenuId != 0).ToList();

                if (!validMenuMappings.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("Role-wise menu mapping operation completed. No valid mappings to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "Role-wise menu mappings deleted successfully" : "No valid menu mappings to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Validate consistency of all items with parent request
                bool isConsistent = validMenuMappings.All(x =>
                    x.BranchId == request.BranchId &&
                    x.RoleId == request.RoleId);

                if (!isConsistent)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    _log.Warn("Inconsistent BranchId or RoleId in menu mapping list.");

                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "All menu mapping items must have the same BranchId and RoleId as the request",
                        400
                    );
                }

                // Insert new role-wise menu mappings
                int insertedCount = 0;
                foreach (var menuMapping in validMenuMappings)
                {
                    var result = _sqlHelper.DML("IU_RoleWiseMenuMappingMaster", CommandType.StoredProcedure, new
                    {
                        @RoleId = menuMapping.RoleId,
                        @BranchId = menuMapping.BranchId,
                        @SubMenuId = menuMapping.SubMenuId,
                        @HospId = globalValues.hospId,
                        @CreatedBy = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
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

                _log.Info($"Inserted {insertedCount} role-wise menu mappings for RoleId={request.RoleId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    $"Role-wise menu mappings updated successfully. {insertedCount} mapping(s) assigned.",
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

        public ServiceResult<IEnumerable<RoleWiseMenuMappingModel>> GetRoleWiseMenuMapping(
           int branchId,
           int roleId)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_RoleWiseMenuMappingMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        @BranchId = branchId,
                        @RoleId = roleId
                    }
                );

                var menuMappings = dataTable?.AsEnumerable().Select(row => new RoleWiseMenuMappingModel
                {
                    IsGranted = row.Field<int>("isGranted"),
                    SubMenuId = row.Field<int>("SubMenuId"),
                    TabId = row.Field<int>("TabId"),
                    SubMenuName = row.Field<string>("SubMenuName") ?? string.Empty,
                    TabName = row.Field<string>("TabName") ?? string.Empty,
                    IsActive = row.Field<int>("IsActive")
                }).ToList() ?? new List<RoleWiseMenuMappingModel>();

                if (!menuMappings.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No role-wise menu mapping found for BranchId={branchId}, RoleId={roleId}");

                    return ServiceResult<IEnumerable<RoleWiseMenuMappingModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {menuMappings.Count} role-wise menu mapping records");

                return ServiceResult<IEnumerable<RoleWiseMenuMappingModel>>.Success(
                    menuMappings,
                    "Info",
                    $"{menuMappings.Count} menu mapping(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<RoleWiseMenuMappingModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


      
        public ServiceResult<string> SaveUpdateUserMenuMaster(
            SaveUserMenuMasterRequest request,
            AllGlobalValues globalValues)
        {
            try
            {
                // Delete existing user menu mappings if IsFirst = 1
                if (request.IsFirst == 1)
                {
                    var deleteResult = _sqlHelper.DML("D_DeleteUserMenuMaster", CommandType.StoredProcedure, new
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

                    _log.Info($"Deleted existing user menu for TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}, RoleId={request.RoleId}");
                }

                // If UserMenus list is empty or null, only delete operation was needed
                if (request.UserMenus == null || !request.UserMenus.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("User menu operation completed. No new menus to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "User menus deleted successfully" : "No user menus to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Filter out items with SubMenuId = 0
                var validUserMenus = request.UserMenus.Where(um => um.SubMenuId != 0).ToList();

                if (!validUserMenus.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("User menu operation completed. No valid menus to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "User menus deleted successfully" : "No valid user menus to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Validate consistency of all items with parent request
                bool isConsistent = validUserMenus.All(x =>
                    x.TypeId == request.TypeId &&
                    x.UserId == request.UserId &&
                    x.BranchId == request.BranchId &&
                    x.RoleId == request.RoleId);

                if (!isConsistent)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    _log.Warn("Inconsistent TypeId, UserId, BranchId, or RoleId in user menu list.");

                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "All user menu items must have the same TypeId, UserId, BranchId, and RoleId as the request",
                        400
                    );
                }

                // Insert new user menu mappings
                int insertedCount = 0;
                foreach (var userMenu in validUserMenus)
                {
                    var result = _sqlHelper.DML("IU_UserMenuMaster", CommandType.StoredProcedure, new
                    {
                        @TypeId = userMenu.TypeId,
                        @UserId = userMenu.UserId,
                        @RoleId = userMenu.RoleId,
                        @BranchId = userMenu.BranchId,
                        @SubMenuId = userMenu.SubMenuId,
                        @HospId = globalValues.hospId,
                        @CreatedBy = globalValues.userId,
                        @IpAddress = globalValues.ipAddress
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

                _log.Info($"Inserted {insertedCount} user menu records for UserId={request.UserId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    $"User menu updated successfully. {insertedCount} menu(s) assigned.",
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

        public ServiceResult<IEnumerable<UserWiseMenuMasterModel>> GetUserWiseMenuMaster(
     int branchId,
     int typeId,
     int userId,
     int roleId)
        {
            try
            {
                var dataTable = _sqlHelper.GetDataTable(
                    "S_UserWiseMenuMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        @BranchId = branchId,
                        @TypeId = typeId,
                        @UserId = userId,
                        @RoleId = roleId
                    }
                );

                var userMenus = dataTable?.AsEnumerable().Select(row => new UserWiseMenuMasterModel
                {
                    IsGranted = row.Field<int>("isGranted"),
                    SubMenuId = row.Field<int>("SubMenuId"),
                    TabId = row.Field<int>("TabId"),
                    SubMenuName = row.Field<string>("SubMenuName") ?? string.Empty,
                    TabName = row.Field<string>("TabName") ?? string.Empty,
                    IsActive = row.Field<int>("IsActive")
                }).ToList() ?? new List<UserWiseMenuMasterModel>();

                if (!userMenus.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No user-wise menu found for BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

                    return ServiceResult<IEnumerable<UserWiseMenuMasterModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {userMenus.Count} user-wise menu records (Granted + Remaining)");

                return ServiceResult<IEnumerable<UserWiseMenuMasterModel>>.Success(
                    userMenus,
                    "Info",
                    $"{userMenus.Count} user-wise menu(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserWiseMenuMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

     
       
    }
}
