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
using StackExchange.Redis;

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
                    @imagePath = request.ImagePath,
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
                        ImagePath = row.Field<string>("ImagePath"),
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

                // Generate cache key for this specific role mapping
                string cacheKey = $"_UserRoleMapping_{branchId}_{typeId}_{userId}";

                // If request list is empty or null, only delete operation is performed
                if (request == null || !request.Any())
                {
                    // Clear cache after delete
                    _distributedCache.Remove(cacheKey);
                    _log.Info($"Cleared cache for key: {cacheKey}");

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

                // Clear cache after successful operation
                _distributedCache.Remove(cacheKey);
                _log.Info($"Cleared cache for key: {cacheKey}");

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
                _log.Info($"GetAssignRoleForUserAuthorization called. BranchId={branchId}, TypeId={typeId}, UserId={userId}");

                // Generate dynamic cache key based on parameters
                string cacheKey = $"_UserRoleMapping_{branchId}_{typeId}_{userId}";

                // Try to get data from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserRoleMappingModel> roles;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserRoleMapping data retrieved from cache. Key={cacheKey}");
                    roles = System.Text.Json.JsonSerializer.Deserialize<List<UserRoleMappingModel>>(cachedData);
                }
                else
                {
                    _log.Info($"UserRoleMapping cache miss. Fetching data from database. Key={cacheKey}");

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

                    roles = dataTable?.AsEnumerable().Select(row => new UserRoleMappingModel
                    {
                        isGranted = row.Field<int>("isGranted"),
                        RoleName = row.Field<string>("RoleName") ?? string.Empty,
                        RoleId = row.Field<int>("RoleId")
                    }).ToList() ?? new List<UserRoleMappingModel>();

                    // Store data in cache with no expiration (permanent until manually cleared)
                    if (roles.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(roles);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"UserRoleMapping data cached permanently. Key={cacheKey}, Count={roles.Count}");
                    }
                }

                if (!roles.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No role authorization data found for UserId={userId}, BranchId={branchId}, TypeId={typeId}");
                    return ServiceResult<IEnumerable<UserRoleMappingModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {roles.Count} role authorization records from cache for UserId={userId}");

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

                // Generate cache key for this specific user right mapping
                string cacheKey = $"_UserRightMapping_{request.BranchId}_{request.TypeId}_{request.UserId}_{request.RoleId}";

                // Clear cache after delete
                _distributedCache.Remove(cacheKey);
                _log.Info($"Cleared cache for key: {cacheKey}");

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
                _log.Info($"GetAssignUserRightMapping called. BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

                // Generate dynamic cache key based on branchId, typeId, userId, and roleId
                string cacheKey = $"_UserRightMapping_{branchId}_{typeId}_{userId}_{roleId}";

                // Try to get data from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserRightMappingModel> userRights;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserRightMapping data retrieved from cache. Key={cacheKey}");
                    userRights = System.Text.Json.JsonSerializer.Deserialize<List<UserRightMappingModel>>(cachedData);
                }
                else
                {
                    _log.Info($"UserRightMapping cache miss. Fetching data from database. Key={cacheKey}");

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

                    userRights = dataTable?.AsEnumerable().Select(row => new UserRightMappingModel
                    {
                        IsGranted = row.Field<int>("isGranted"),
                        UserRightName = row.Field<string>("UserRightName") ?? string.Empty,
                        Description = row.Field<string>("Description") ?? string.Empty,
                        UserRightId = row.Field<int>("UserRightId")
                    }).ToList() ?? new List<UserRightMappingModel>();

                    // Store data in cache with no expiration
                    if (userRights.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(userRights);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"UserRightMapping data cached permanently. Key={cacheKey}, Count={userRights.Count}");
                    }
                }

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

                _log.Info($"Retrieved {userRights.Count} user rights mapping records from cache");

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

                // Generate cache key for this specific dashboard user right mapping
                string cacheKey = $"_DashboardUserRightMapping_{request.BranchId}_{request.TypeId}_{request.UserId}_{request.RoleId}";

                // Clear cache after delete
                _distributedCache.Remove(cacheKey);
                _log.Info($"Cleared cache for key: {cacheKey}");

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
                _log.Info($"GetAssignDashBoardUserRight called. BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

                // Generate dynamic cache key based on branchId, typeId, userId, and roleId
                string cacheKey = $"_DashboardUserRightMapping_{branchId}_{typeId}_{userId}_{roleId}";

                // Try to get data from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<DashboardUserRightMappingModel> dashboardRights;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"DashboardUserRightMapping data retrieved from cache. Key={cacheKey}");
                    dashboardRights = System.Text.Json.JsonSerializer.Deserialize<List<DashboardUserRightMappingModel>>(cachedData);
                }
                else
                {
                    _log.Info($"DashboardUserRightMapping cache miss. Fetching data from database. Key={cacheKey}");

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

                    dashboardRights = dataTable?.AsEnumerable().Select(row => new DashboardUserRightMappingModel
                    {
                        IsGranted = row.Field<int>("isGranted"),
                        UserRightName = row.Field<string>("UserRightName") ?? string.Empty,
                        Details = row.Field<string>("Details") ?? string.Empty,
                        UserRightId = row.Field<int>("UserRightId")
                    }).ToList() ?? new List<DashboardUserRightMappingModel>();

                    // Store data in cache with no expiration
                    if (dashboardRights.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(dashboardRights);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"DashboardUserRightMapping data cached permanently. Key={cacheKey}, Count={dashboardRights.Count}");
                    }
                }

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

                _log.Info($"Retrieved {dashboardRights.Count} dashboard user rights mapping records from cache");

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

                // Fetch data from database using stored procedure
                var dataTable = _sqlHelper.GetDataTable(
                    "S_GetNavigationTabMaster",
                    CommandType.StoredProcedure
                );

                var navigationTabs = dataTable?.AsEnumerable().Select(row => new NavigationTabMasterModel
                {
                    TabId = row.Field<int>("TabId"),
                    TabName = row.Field<string>("TabName") ?? string.Empty,
                    FaIconId = row.Field<int>("FaIconId"),
                    IsActive = row.Field<int>("IsActive")
                }).ToList() ?? new List<NavigationTabMasterModel>();

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
                _log.Info($"Retrieved {navigationTabs.Count} navigation tab(s) from database");

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
                    TabName = row.Field<string>("TabName") ?? string.Empty,
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
                        @BranchId = 1,
                        @RoleId = request.RoleId
                    },
                    new
                    {
                        result = 0
                    });

                    _log.Info($"Deleted existing role-wise menu mappings for BranchId={request.BranchId}, RoleId={request.RoleId}");
                }

                // Generate cache key for this specific mapping
                string cacheKey = $"_RoleWiseMenuMapping_{1}_{request.RoleId}";

                // If MenuMappings list is empty or null, only delete operation was needed
                if (request.MenuMappings == null || !request.MenuMappings.Any())
                {
                    // Clear cache after delete
                    _distributedCache.Remove(cacheKey);
                    _log.Info($"Cleared cache for key: {cacheKey}");

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
                    // Clear cache
                    _distributedCache.Remove(cacheKey);
                    _log.Info($"Cleared cache for key: {cacheKey}");

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
                        @BranchId = 1,
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

                // Clear cache after successful operation
                _distributedCache.Remove(cacheKey);
                _log.Info($"Cleared cache for key: {cacheKey}");

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
                _log.Info($"GetRoleWiseMenuMapping called. BranchId={1}, RoleId={roleId}");

                // Generate dynamic cache key based on branchId and roleId
                string cacheKey = $"_RoleWiseMenuMapping_{1}_{roleId}";

                // Try to get data from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<RoleWiseMenuMappingModel> menuMappings;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"RoleWiseMenuMapping data retrieved from cache. Key={cacheKey}");
                    menuMappings = System.Text.Json.JsonSerializer.Deserialize<List<RoleWiseMenuMappingModel>>(cachedData);
                }
                else
                {
                    _log.Info($"RoleWiseMenuMapping cache miss. Fetching data from database. Key={cacheKey}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_RoleWiseMenuMappingMaster",
                        CommandType.StoredProcedure,
                        new
                        {
                            @BranchId = 1,
                            @RoleId = roleId
                        }
                    );

                    menuMappings = dataTable?.AsEnumerable().Select(row => new RoleWiseMenuMappingModel
                    {
                        IsGranted = row.Field<int>("isGranted"),
                        SubMenuId = row.Field<int>("SubMenuId"),
                        TabId = row.Field<int>("TabId"),
                        SubMenuName = row.Field<string>("SubMenuName") ?? string.Empty,
                        TabName = row.Field<string>("TabName") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<RoleWiseMenuMappingModel>();

                    // Store data in cache with no expiration
                    if (menuMappings.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(menuMappings);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"RoleWiseMenuMapping data cached permanently. Key={cacheKey}, Count={menuMappings.Count}");
                    }
                }

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

                _log.Info($"Retrieved {menuMappings.Count} role-wise menu mapping records from cache");

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

                // Generate cache key for this specific user menu mapping
                string cacheKey = $"_UserWiseMenuMapping_{request.BranchId}_{request.TypeId}_{request.UserId}_{request.RoleId}";
                string cacheKey2 = $"_UserTabMenu_{request.BranchId}_{request.RoleId}_{request.UserId}";
                // Clear cache after delete
                _distributedCache.Remove(cacheKey);
                _distributedCache.Remove(cacheKey2);
                _log.Info($"Cleared cache for key: {cacheKey},{cacheKey2}");

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
                _log.Info($"GetUserWiseMenuMaster called. BranchId={branchId}, TypeId={typeId}, UserId={userId}, RoleId={roleId}");

                // Generate dynamic cache key based on branchId, typeId, userId, and roleId
                string cacheKey = $"_UserWiseMenuMapping_{branchId}_{typeId}_{userId}_{roleId}";

                // Try to get data from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserWiseMenuMasterModel> userMenus;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserWiseMenuMaster data retrieved from cache. Key={cacheKey}");
                    userMenus = System.Text.Json.JsonSerializer.Deserialize<List<UserWiseMenuMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"UserWiseMenuMaster cache miss. Fetching data from database. Key={cacheKey}");

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

                    userMenus = dataTable?.AsEnumerable().Select(row => new UserWiseMenuMasterModel
                    {
                        IsGranted = row.Field<int>("isGranted"),
                        SubMenuId = row.Field<int>("SubMenuId"),
                        TabId = row.Field<int>("TabId"),
                        SubMenuName = row.Field<string>("SubMenuName") ?? string.Empty,
                        TabName = row.Field<string>("TabName") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<UserWiseMenuMasterModel>();

                    // Store data in cache with no expiration
                    if (userMenus.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(userMenus);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"UserWiseMenuMaster data cached permanently. Key={cacheKey}, Count={userMenus.Count}");
                    }
                }

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

                _log.Info($"Retrieved {userMenus.Count} user-wise menu records (Granted + Remaining) from cache");

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

        public ServiceResult<string> SaveUpdateUserCorporateMapping(
     SaveUserCorporateMappingRequest request,
     AllGlobalValues globalValues)
        {
            try
            {
                // Delete existing user corporate mappings if IsFirst = 1
                if (request.IsFirst == 1)
                {
                    var deleteResult = _sqlHelper.DML("D_DeleteUserCorporateMapping", CommandType.StoredProcedure, new
                    {
                        @TypeId = request.TypeId,
                        @UserId = request.UserId,
                        @BranchId = request.BranchId
                    },
                    new
                    {
                        result = 0
                    });

                    _log.Info($"Deleted existing user corporate mapping for TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}");
                }

                // If UserCorporates list is empty or null, only delete operation was needed
                if (request.UserCorporates == null || !request.UserCorporates.Any())
                {
                    // Clear cache after delete
                    string cacheKey = $"_UserCorporateMapping_{request.BranchId}_{request.TypeId}_{request.UserId}";
                    _distributedCache.Remove(cacheKey);
                    _log.Info($"Cleared cache for key: {cacheKey}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("User corporate mapping operation completed. No new mappings to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "User corporate mappings deleted successfully" : "No corporate mappings to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Filter out items with CorporateId = 0
                var validUserCorporates = request.UserCorporates.Where(uc => uc.CorporateId != 0).ToList();

                if (!validUserCorporates.Any())
                {
                    // Clear cache
                    string cacheKey = $"_UserCorporateMapping_{request.BranchId}_{request.TypeId}_{request.UserId}";
                    _distributedCache.Remove(cacheKey);
                    _log.Info($"Cleared cache for key: {cacheKey}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("User corporate mapping operation completed. No valid mappings to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "User corporate mappings deleted successfully" : "No valid corporate mappings to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Validate consistency of all items with parent request
                bool isConsistent = validUserCorporates.All(x =>
                    x.TypeId == request.TypeId &&
                    x.UserId == request.UserId &&
                    x.BranchId == request.BranchId);

                if (!isConsistent)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    _log.Warn("Inconsistent TypeId, UserId, or BranchId in user corporate mapping list.");

                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "All user corporate mapping items must have the same TypeId, UserId, and BranchId as the request",
                        400
                    );
                }

                // Insert new user corporate mappings
                int insertedCount = 0;
                foreach (var userCorporate in validUserCorporates)
                {
                    var result = _sqlHelper.DML("IU_UserCorporateMapping", CommandType.StoredProcedure, new
                    {
                        @hospId = globalValues.hospId,
                        @TypeId = userCorporate.TypeId,
                        @UserId = userCorporate.UserId,
                        @BranchId = userCorporate.BranchId,
                        @CorporateId = userCorporate.CorporateId,
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

                // Clear cache after successful operation
                string clearCacheKey = $"_UserCorporateMapping_{request.BranchId}_{request.TypeId}_{request.UserId}";
                _distributedCache.Remove(clearCacheKey);
                _log.Info($"Cleared cache for key: {clearCacheKey}");

                _log.Info($"Inserted {insertedCount} user corporate mapping records for UserId={request.UserId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    $"User corporate mapping updated successfully. {insertedCount} corporate(s) assigned.",
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

        public ServiceResult<IEnumerable<UserWiseCorporateMappingModel>> GetUserWiseCorporateMapping(
            int branchId,
            int typeId,
            int userId)
        {
            try
            {
                _log.Info($"GetUserWiseCorporateMapping called. BranchId={branchId}, TypeId={typeId}, UserId={userId}");

                // Generate dynamic cache key based on branchId, typeId, and userId
                string cacheKey = $"_UserCorporateMapping_{branchId}_{typeId}_{userId}";

                // Try to get data from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserWiseCorporateMappingModel> userCorporates;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserCorporateMapping data retrieved from cache. Key={cacheKey}");
                    userCorporates = System.Text.Json.JsonSerializer.Deserialize<List<UserWiseCorporateMappingModel>>(cachedData);
                }
                else
                {
                    _log.Info($"UserCorporateMapping cache miss. Fetching data from database. Key={cacheKey}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetRemainingAssignCorporateForUserAuthorization",
                        CommandType.StoredProcedure,
                        new
                        {
                            @BranchId = branchId,
                            @TypeId = typeId,
                            @UserId = userId
                        }
                    );

                    userCorporates = dataTable?.AsEnumerable().Select(row => new UserWiseCorporateMappingModel
                    {
                        IsGranted = row.Field<int>("isGranted"),
                        CorporateId = row.Field<int>("CorporateId"),
                        CorporateName = row.Field<string>("CorporateName") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive")
                    }).ToList() ?? new List<UserWiseCorporateMappingModel>();

                    // Store data in cache with no expiration
                    if (userCorporates.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(userCorporates);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"UserCorporateMapping data cached permanently. Key={cacheKey}, Count={userCorporates.Count}");
                    }
                }

                if (!userCorporates.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No user corporate mapping found for BranchId={branchId}, TypeId={typeId}, UserId={userId}");

                    return ServiceResult<IEnumerable<UserWiseCorporateMappingModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {userCorporates.Count} user corporate mapping records (Granted + Remaining) from cache");

                return ServiceResult<IEnumerable<UserWiseCorporateMappingModel>>.Success(
                    userCorporates,
                    "Info",
                    $"{userCorporates.Count} user corporate mapping(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserWiseCorporateMappingModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }


        public ServiceResult<string> SaveUpdateUserBedMapping(
    SaveUserBedMappingRequest request,
    AllGlobalValues globalValues)
        {
            try
            {
                // Delete existing user bed mappings if IsFirst = 1
                if (request.IsFirst == 1)
                {
                    var deleteResult = _sqlHelper.DML("D_DeleteUserBedMapping", CommandType.StoredProcedure, new
                    {
                        @TypeId = request.TypeId,
                        @UserId = request.UserId,
                        @BranchId = request.BranchId
                    },
                    new
                    {
                        result = 0
                    });

                    _log.Info($"Deleted existing user bed mapping for TypeId={request.TypeId}, UserId={request.UserId}, BranchId={request.BranchId}");
                }

                // Generate cache key for this specific bed mapping
                string cacheKey = $"_UserBedMapping_{request.BranchId}_{request.TypeId}_{request.UserId}";

                // If UserBeds list is empty or null, only delete operation was needed
                if (request.UserBeds == null || !request.UserBeds.Any())
                {
                    // Clear cache after delete
                    _distributedCache.Remove(cacheKey);
                    _log.Info($"Cleared cache for key: {cacheKey}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("User bed mapping operation completed. No new mappings to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "User bed mappings deleted successfully" : "No bed mappings to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Filter out items with ServiceItemId = 0
                var validUserBeds = request.UserBeds.Where(ub => ub.ServiceItemId != 0).ToList();

                if (!validUserBeds.Any())
                {
                    // Clear cache
                    _distributedCache.Remove(cacheKey);
                    _log.Info($"Cleared cache for key: {cacheKey}");

                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.IsFirst == 1 ? "DATA_DELETED_SUCCESSFULLY" : "DATA_SAVED_SUCCESSFULLY"
                    );
                    _log.Info("User bed mapping operation completed. No valid mappings to insert.");

                    return ServiceResult<string>.Success(
                        request.IsFirst == 1 ? "User bed mappings deleted successfully" : "No valid bed mappings to save",
                        alert.Type,
                        alert.Message,
                        200
                    );
                }

                // Validate consistency of all items with parent request
                bool isConsistent = validUserBeds.All(x =>
                    x.TypeId == request.TypeId &&
                    x.UserId == request.UserId &&
                    x.BranchId == request.BranchId);

                if (!isConsistent)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("INVALID_PARAMETER");
                    _log.Warn("Inconsistent TypeId, UserId, or BranchId in user bed mapping list.");

                    return ServiceResult<string>.Failure(
                        alert.Type,
                        "All user bed mapping items must have the same TypeId, UserId, and BranchId as the request",
                        400
                    );
                }

                // Insert new user bed mappings
                int insertedCount = 0;
                foreach (var userBed in validUserBeds)
                {
                    var result = _sqlHelper.DML("IU_UserBedMapping", CommandType.StoredProcedure, new
                    {
                        @hospId = globalValues.hospId,
                        @TypeId = userBed.TypeId,
                        @UserId = userBed.UserId,
                        @BranchId = userBed.BranchId,
                        @ServiceItemId = userBed.ServiceItemId,
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

                // Clear cache after successful operation
                _distributedCache.Remove(cacheKey);
                _log.Info($"Cleared cache for key: {cacheKey}");

                _log.Info($"Inserted {insertedCount} user bed mapping records for UserId={request.UserId}");

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                return ServiceResult<string>.Success(
                    $"User bed mapping updated successfully. {insertedCount} bed(s) assigned.",
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

        public ServiceResult<IEnumerable<UserWiseBedMappingModel>> GetUserWiseBedMapping(
            int branchId,
            int typeId,
            int userId)
        {
            try
            {
                _log.Info($"GetUserWiseBedMapping called. BranchId={branchId}, TypeId={typeId}, UserId={userId}");

                // Generate dynamic cache key based on branchId, typeId, and userId
                string cacheKey = $"_UserBedMapping_{branchId}_{typeId}_{userId}";

                // Try to get data from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<UserWiseBedMappingModel> userBeds;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"UserBedMapping data retrieved from cache. Key={cacheKey}");
                    userBeds = System.Text.Json.JsonSerializer.Deserialize<List<UserWiseBedMappingModel>>(cachedData);
                }
                else
                {
                    _log.Info($"UserBedMapping cache miss. Fetching data from database. Key={cacheKey}");

                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetRemainingAssignBedForUserAuthorization",
                        CommandType.StoredProcedure,
                        new
                        {
                            @BranchId = branchId,
                            @TypeId = typeId,
                            @UserId = userId
                        }
                    );

                    userBeds = dataTable?.AsEnumerable().Select(row => new UserWiseBedMappingModel
                    {
                        IsGranted = row.Field<int>("isGranted"),
                        ServiceItemId = row.Field<int>("ServiceItemId"),
                        Name = row.Field<string>("Name") ?? string.Empty,
                    }).ToList() ?? new List<UserWiseBedMappingModel>();

                    // Store data in cache with no expiration
                    if (userBeds.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(userBeds);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"UserBedMapping data cached permanently. Key={cacheKey}, Count={userBeds.Count}");
                    }
                }

                if (!userBeds.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No user bed mapping found for BranchId={branchId}, TypeId={typeId}, UserId={userId}");

                    return ServiceResult<IEnumerable<UserWiseBedMappingModel>>.Failure(
                        alert.Type,
                        alert.Message,
                        404
                    );
                }

                _log.Info($"Retrieved {userBeds.Count} user bed mapping records (Granted + Remaining) from cache");

                return ServiceResult<IEnumerable<UserWiseBedMappingModel>>.Success(
                    userBeds,
                    "Info",
                    $"{userBeds.Count} user bed mapping(s) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<UserWiseBedMappingModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<BranchMasterResponse> CreateUpdateBranchMaster(BranchMasterRequest request, AllGlobalValues globalValues)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@hospId", globalValues.hospId),
            new SqlParameter("@branchId", request.BranchId),
            new SqlParameter("@branchName", request.BranchName),
            new SqlParameter("@branchCode", request.BranchCode),
            new SqlParameter("@email", request.Email ?? (object)DBNull.Value),
            new SqlParameter("@contactNo1", request.ContactNo1),
            new SqlParameter("@contactNo2", request.ContactNo2 ?? (object)DBNull.Value),
            new SqlParameter("@address", request.Address ?? (object)DBNull.Value),
            new SqlParameter("@isActive", request.IsActive),
            new SqlParameter("@fYStartFrom", request.FYStartFrom),
            new SqlParameter("@defaultCountryId", request.DefaultCountryId),
            new SqlParameter("@defaultStateId", request.DefaultStateId),
            new SqlParameter("@defaultDistrictId", request.DefaultDistrictId),
            new SqlParameter("@defaultCityId", request.DefaultCityId),
            new SqlParameter("@defaultInsuranceCompanyId", request.DefaultInsuranceCompanyId),
            new SqlParameter("@defaultCorporateId", request.DefaultCorporateId),
            new SqlParameter("@userId", globalValues.userId),
            new SqlParameter("@IpAddress", globalValues.ipAddress),
            new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output }
                };

                long result = _sqlHelper.RunProcedureInsert("IU_BranchMaster", parameters);

                // Clear cache after successful operation
                _distributedCache.Remove("_BranchMaster_All");

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate branch name or code attempted: {request.BranchName}");
                    return ServiceResult<BranchMasterResponse>.Failure(
                        alert.Type,
                        "Branch Name or Branch Code already exists",
                        409
                    );
                }

                if (request.BranchId == 0)
                {
                    var responseData = new BranchMasterResponse { BranchId = (int)result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_SAVED_SUCCESSFULLY");
                    _log.Info($"Branch created successfully. BranchId={result}");
                    return ServiceResult<BranchMasterResponse>.Success(
                        responseData,
                        alert.Type,
                        alert.Message,
                        201
                    );
                }
                else
                {
                    var responseData = new BranchMasterResponse { BranchId = (int)result };
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_UPDATED_SUCCESSFULLY");
                    _log.Info($"Branch updated successfully. BranchId={result}");
                    return ServiceResult<BranchMasterResponse>.Success(
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
                return ServiceResult<BranchMasterResponse>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<IEnumerable<BranchMasterModel>> GetBranchDetails(int? branchId = null)
        {
            try
            {
                _log.Info($"GetBranchDetails called. BranchId={branchId?.ToString() ?? "All"}");

                string cacheKey = "_BranchMaster_All";

                // Try to get all branches from cache
                var cachedData = _distributedCache.GetString(cacheKey);
                List<BranchMasterModel> allBranches;

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _log.Info($"BranchMaster data retrieved from cache. Key={cacheKey}");
                    allBranches = System.Text.Json.JsonSerializer.Deserialize<List<BranchMasterModel>>(cachedData);
                }
                else
                {
                    _log.Info($"BranchMaster cache miss. Fetching all data from database. Key={cacheKey}");

                    // Fetch ALL branches from database (NO parameters - SP returns everything)
                    var dataTable = _sqlHelper.GetDataTable(
                        "S_GetBranchDetails",
                        CommandType.StoredProcedure
                    );

                    allBranches = dataTable?.AsEnumerable().Select(row => new BranchMasterModel
                    {
                        BranchId = row.Field<int>("BranchId"),
                        BranchName = row.Field<string>("BranchName") ?? string.Empty,
                        BranchCode = row.Field<string>("BranchCode") ?? string.Empty,
                        Email = row.Field<string>("Email") ?? string.Empty,
                        ContactNo1 = row.Field<string>("ContactNo1") ?? string.Empty,
                        ContactNo2 = row.Field<string>("ContactNo2") ?? string.Empty,
                        Address = row.Field<string>("Address") ?? string.Empty,
                        IsActive = row.Field<int>("IsActive"),
                        FYStartMonth = row.Field<string>("FYStartMonth") ?? string.Empty,
                        DefaultCountryId = row.Field<int>("DefaultCountryId"),
                        DefaultStateId = row.Field<int>("DefaultStateId"),
                        DefaultDistrictId = row.Field<int>("DefaultDistrictId"),
                        DefaultCityId = row.Field<int>("DefaultCityId"),
                        DefaultInsuranceCompanyId = row.Field<int>("DefaultInsuranceCompanyId"),
                        DefaultCorporateId = row.Field<int>("DefaultCorporateId")
                    }).ToList() ?? new List<BranchMasterModel>();

                    // Store ALL branches in cache (no expiration)
                    if (allBranches.Any())
                    {
                        var serialized = System.Text.Json.JsonSerializer.Serialize(allBranches);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            // No expiration - cache persists until manually cleared
                            AbsoluteExpiration = null,
                            SlidingExpiration = null
                        };
                        _distributedCache.SetString(cacheKey, serialized, cacheOptions);
                        _log.Info($"All BranchMaster data cached permanently. Key={cacheKey}, Count={allBranches.Count}");
                    }
                }

                // Filter in memory based on branchId parameter (always from cache)
                List<BranchMasterModel> filteredBranches;
                if (branchId.HasValue)
                {
                    _log.Info($"Filtering cached data by BranchId: {branchId.Value}");
                    filteredBranches = allBranches.Where(b => b.BranchId == branchId.Value).ToList();
                }
                else
                {
                    _log.Info("Returning all cached branches");
                    filteredBranches = allBranches;
                }

                if (!filteredBranches.Any())
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Info($"No branches found for BranchId: {branchId?.ToString() ?? "All"}");
                    return ServiceResult<IEnumerable<BranchMasterModel>>.Failure(
                        alert.Type,
                        branchId.HasValue
                            ? $"Branch not found for BranchId: {branchId.Value}"
                            : "No branches found",
                        404
                    );
                }

                _log.Info($"Retrieved {filteredBranches.Count} branch(es) from cache");

                return ServiceResult<IEnumerable<BranchMasterModel>>.Success(
                    filteredBranches,
                    "Info",
                    $"{filteredBranches.Count} branch(es) retrieved successfully",
                    200
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<IEnumerable<BranchMasterModel>>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }



        public ServiceResult<int> CreateUpdateStateMaster(CreateUpdateStateMasterRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdateStateMaster called. StateId={request.StateId}, CountryId={request.CountryId}, StateName={request.StateName}");

                var dataTable = _sqlHelper.GetDataTable(
                    "IU_StateMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        StateId = request.StateId,
                        CountryId = request.CountryId,
                        StateName = request.StateName,
                        IsActive = request.IsActive,
                        UserId = globalValues.userId,
                        IpAddress = globalValues.ipAddress
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    _log.Error("No result returned from stored procedure");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                int result = Convert.ToInt32(dataTable.Rows[0]["Result"]);

                // Clear all state-related cache keys
                ClearStateMasterCache(request.CountryId);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate state name: {request.StateName} for CountryId={request.CountryId}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        $"State '{request.StateName}' already exists for this country",
                        409
                    );
                }

                if (result == -2)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"StateId not found: {request.StateId}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        "State record not found",
                        404
                    );
                }

                if (result > 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.StateId <= 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );

                    _log.Info($"State {(request.StateId <= 0 ? "created" : "updated")} successfully. StateId={result}. Cache cleared.");

                    return ServiceResult<int>.Success(
                        result,
                        alert.Type,
                        alert.Message,
                        request.StateId <= 0 ? 201 : 200
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_FAILED");
                _log.Error($"Operation failed with result: {result}");
                return ServiceResult<int>.Failure(
                    alert1.Type,
                    alert1.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<int>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<int> CreateUpdateDistrictMaster(CreateUpdateDistrictMasterRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdateDistrictMaster called. DistrictId={request.DistrictId}, StateId={request.StateId}, DistrictName={request.DistrictName}");

                var dataTable = _sqlHelper.GetDataTable(
                    "IU_DistrictMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        DistrictId = request.DistrictId,
                        StateId = request.StateId,
                        CountryId = request.CountryId,
                        DistrictName = request.DistrictName,
                        IsActive = request.IsActive,
                        UserId = globalValues.userId,
                        IpAddress = globalValues.ipAddress
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    _log.Error("No result returned from stored procedure");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                int result = Convert.ToInt32(dataTable.Rows[0]["Result"]);

                // Clear all district-related cache keys
                ClearDistrictMasterCache(request.StateId);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate district name: {request.DistrictName} for StateId={request.StateId}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        $"District '{request.DistrictName}' already exists for this state",
                        409
                    );
                }

                if (result == -2)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"DistrictId not found: {request.DistrictId}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        "District record not found",
                        404
                    );
                }

                if (result > 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.DistrictId <= 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );

                    _log.Info($"District {(request.DistrictId <= 0 ? "created" : "updated")} successfully. DistrictId={result}. Cache cleared.");

                    return ServiceResult<int>.Success(
                        result,
                        alert.Type,
                        alert.Message,
                        request.DistrictId <= 0 ? 201 : 200
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_FAILED");
                _log.Error($"Operation failed with result: {result}");
                return ServiceResult<int>.Failure(
                    alert1.Type,
                    alert1.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<int>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        public ServiceResult<int> CreateUpdateCityMaster(CreateUpdateCityMasterRequest request, AllGlobalValues globalValues)
        {
            try
            {
                _log.Info($"CreateUpdateCityMaster called. CityId={request.CityId}, DistrictId={request.DistrictId}, CityName={request.CityName}");

                var dataTable = _sqlHelper.GetDataTable(
                    "IU_CityMaster",
                    CommandType.StoredProcedure,
                    new
                    {
                        CityId = request.CityId,
                        DistrictId = request.DistrictId,
                        StateId = request.StateId,
                        CountryId = request.CountryId,
                        CityName = request.CityName,
                        IsActive = request.IsActive,
                        UserId = globalValues.userId,
                        IpAddress = globalValues.ipAddress
                    }
                );

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                    _log.Error("No result returned from stored procedure");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        alert.Message,
                        500
                    );
                }

                int result = Convert.ToInt32(dataTable.Rows[0]["Result"]);

                // Clear all city-related cache keys
                ClearCityMasterCache(request.DistrictId);

                if (result == -1)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("RECORD_ALREADY_EXISTS");
                    _log.Warn($"Duplicate city name: {request.CityName} for DistrictId={request.DistrictId}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        $"City '{request.CityName}' already exists for this district",
                        409
                    );
                }

                if (result == -2)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode("DATA_NOT_FOUND");
                    _log.Warn($"CityId not found: {request.CityId}");
                    return ServiceResult<int>.Failure(
                        alert.Type,
                        "City record not found",
                        404
                    );
                }

              

                if (result > 0)
                {
                    var alert = _messageService.GetMessageAndTypeByAlertCode(
                        request.CityId <= 0 ? "DATA_SAVED_SUCCESSFULLY" : "DATA_UPDATED_SUCCESSFULLY"
                    );

                    _log.Info($"City {(request.CityId <= 0 ? "created" : "updated")} successfully. CityId={result}. Cache cleared.");

                    return ServiceResult<int>.Success(
                        result,
                        alert.Type,
                        alert.Message,
                        request.CityId <= 0 ? 201 : 200
                    );
                }

                var alert1 = _messageService.GetMessageAndTypeByAlertCode("OPERATION_FAILED");
                _log.Error($"Operation failed with result: {result}");
                return ServiceResult<int>.Failure(
                    alert1.Type,
                    alert1.Message,
                    500
                );
            }
            catch (Exception ex)
            {
                LogErrors.WriteErrorLog(ex, $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}");
                var alert = _messageService.GetMessageAndTypeByAlertCode("SERVER_ERROR_FOUND");
                return ServiceResult<int>.Failure(
                    alert.Type,
                    alert.Message,
                    500
                );
            }
        }

        // Helper methods to clear cache
        private void ClearStateMasterCache(int countryId)
        {
            try
            {
                // Clear all possible cache keys for this country's states
                _distributedCache.Remove($"_StateMaster_Country{countryId}_All");
                _distributedCache.Remove($"_StateMaster_Country{countryId}_1");
                _distributedCache.Remove($"_StateMaster_Country{countryId}_0");
                _log.Info($"Cleared StateMaster cache for CountryId={countryId}");
            }
            catch (Exception ex)
            {
                _log.Error($"Error clearing StateMaster cache: {ex.Message}");
            }
        }

        private void ClearDistrictMasterCache(int stateId)
        {
            try
            {
                // Clear all possible cache keys for this state's districts
                _distributedCache.Remove($"_DistrictMaster_State{stateId}_All");
                _distributedCache.Remove($"_DistrictMaster_State{stateId}_1");
                _distributedCache.Remove($"_DistrictMaster_State{stateId}_0");
                _log.Info($"Cleared DistrictMaster cache for StateId={stateId}");
            }
            catch (Exception ex)
            {
                _log.Error($"Error clearing DistrictMaster cache: {ex.Message}");
            }
        }

        private void ClearCityMasterCache(int districtId)
        {
            try
            {
                // Clear all possible cache keys for this district's cities
                _distributedCache.Remove($"_CityMaster_District{districtId}_All");
                _distributedCache.Remove($"_CityMaster_District{districtId}_1");
                _distributedCache.Remove($"_CityMaster_District{districtId}_0");
                _log.Info($"Cleared CityMaster cache for DistrictId={districtId}");
            }
            catch (Exception ex)
            {
                _log.Error($"Error clearing CityMaster cache: {ex.Message}");
            }
        }
    }
}
