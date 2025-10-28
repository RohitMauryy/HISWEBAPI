using System.Collections.Generic;
using System.Data;
using System.Linq;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Models.Admin;

namespace HISWEBAPI.Repositories.Implementations
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;

        public HomeRepository(ICustomSqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public IEnumerable<BranchModel> GetActiveBranchList()
        {
            var dataTable = _sqlHelper.GetDataTable(
                "S_GetActiveBranchList",
                CommandType.StoredProcedure
            );

            return dataTable?.AsEnumerable().Select(row => new BranchModel
            {
                branchId = row.Field<int>("BranchId"),
                branchName = row.Field<string>("BranchName")
            }).ToList() ?? new List<BranchModel>();
        }

       
    }
}
