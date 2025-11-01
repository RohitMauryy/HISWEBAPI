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
                    @mappingBranch = request.MappingBranch,
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
                    MappingBranch = row.Field<string>("MappingBranch"),
                    IsActive = row.Field<int>("IsActive")
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
    }
}
