using HISWEBAPI.Models;
using HISWEBAPI.Interface;
using PMS.DAL;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;

namespace HISWEBAPI.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;

        public HomeRepository(ICustomSqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public async Task<IEnumerable<BranchModel>> GetActiveBranchListAsync()
        {
            var dataTable = await _sqlHelper.GetDataTableAsync(
                "S_GetActiveBranchList",
                CommandType.StoredProcedure
            );

            return dataTable?.AsEnumerable().Select(row => new BranchModel
            {
                branchId = row.Field<int>("BranchId"),
                branchName = row.Field<string>("BranchName")
            }).ToList() ?? new List<BranchModel>();
        }

        public async Task<long> UserLoginAsync(int branchId, string userName, string password)
        {
            var result = await _sqlHelper.ExecuteScalarAsync(
                "sp_S_Login",
                CommandType.StoredProcedure,
                new { BranchId = branchId, UserName = userName, Password = password }
            );

            long userId = 0; // Initialize here
            if (result != null && long.TryParse(result.ToString(), out userId))
                return userId;

            return 0; // invalid credentials
        }
    }
}