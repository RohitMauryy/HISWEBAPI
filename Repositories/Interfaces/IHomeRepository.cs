using System.Collections.Generic;
using HISWEBAPI.Models;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IHomeRepository
    {
        IEnumerable<BranchModel> GetActiveBranchList();
        IEnumerable<PickListModel> GetPickListMaster(string fieldName);
    }
}