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

    }
}
