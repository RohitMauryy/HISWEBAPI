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

namespace HISWEBAPI.Repositories.Implementations
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;

        public AdminRepository(ICustomSqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
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
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false, message = "Role Name Already Exists." });

                if (request.RoleId == 0)
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true, message = "Role Saved Successfully" });
                else
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = true, message = "Role Updated Successfully" });
            }
            catch (Exception ex)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { result = false, message = "Server Error found" });
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