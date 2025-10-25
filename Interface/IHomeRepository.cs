using System.Collections.Generic;
using HISWEBAPI.Models;

namespace HISWEBAPI.Interface
{
    public interface IHomeRepository
    {
        IEnumerable<BranchModel> GetActiveBranchList();
        long UserLogin(int branchId, string userName, string password);
    }
}
