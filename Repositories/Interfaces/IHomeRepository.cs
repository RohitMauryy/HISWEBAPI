using System.Collections.Generic;
using HISWEBAPI.Models.Admin;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IHomeRepository
    {
        IEnumerable<BranchModel> GetActiveBranchList();
        long UserLogin(int branchId, string userName, string password);
    }
}
