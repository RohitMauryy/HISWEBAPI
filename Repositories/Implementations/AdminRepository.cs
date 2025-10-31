using System.Collections.Generic;
using System.Data;
using System.Linq;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;
using Microsoft.Data.SqlClient;
using HISWEBAPI.Utilities;
using HISWEBAPI.Exceptions;
using HISWEBAPI.Services;

namespace HISWEBAPI.Repositories.Implementations
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;
        private readonly IResponseMessageService _messageService;


        public AdminRepository(ICustomSqlHelper sqlHelper, IResponseMessageService messageService)
        {
            _sqlHelper = sqlHelper;
            _messageService = messageService;

        }

        private (string Type, string Message) GetAlert(string alertCode)
        {
            return _messageService.GetMessageAndTypeByAlertCode(alertCode);
        }

        public string CreateUpdateRoleMaster(RoleMasterRequest request, AllGlobalValues globalValues)
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
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false, messageType = GetAlert("RECORD_ALREADY_EXISTS").Type, message = GetAlert("RECORD_ALREADY_EXISTS").Message  });

                if (request.RoleId == 0)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true, messageType = GetAlert("DATA_SAVED_SUCCESSFULLY").Type, message = GetAlert("DATA_SAVED_SUCCESSFULLY").Message });
                else
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true, messageType = GetAlert("DATA_UPDATED_SUCCESSFULLY").Type, message = GetAlert("DATA_UPDATED_SUCCESSFULLY").Message });
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false, messageType = GetAlert("SERVER_ERROR_FOUND").Type, message = GetAlert("SERVER_ERROR_FOUND").Message });
            }
        }

        public IEnumerable<RoleMasterModel> RoleMasterList()
        {
            var dataTable = _sqlHelper.GetDataTable(
                "S_GetRoleList",
                CommandType.StoredProcedure,
                new
                {
                    @roleName = ""
                }
            );

            return dataTable?.AsEnumerable().Select(row => new RoleMasterModel
            {
                RoleId = row.Field<int>("RoleId"),
                RoleName = row.Field<string>("RoleName"),
                MappingBranch = row.Field<string>("MappingBranch"),
                IsActive = row.Field<int>("IsActive")
            }).ToList() ?? new List<RoleMasterModel>();
        }

       
    }
}