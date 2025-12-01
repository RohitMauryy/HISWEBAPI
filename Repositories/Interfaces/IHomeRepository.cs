using System.Collections.Generic;
using HISWEBAPI.Models;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IHomeRepository
    {
        ServiceResult<IEnumerable<BranchModel>> GetActiveBranchList();
        ServiceResult<IEnumerable<PickListModel>> GetPickListMaster(string fieldName);
        ServiceResult<AllGlobalValues> GetAllGlobalValues();
        ServiceResult<string> ClearAllCache();
    }
}