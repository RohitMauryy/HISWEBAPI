using System.Collections.Generic;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        string CreateUpdateRoleMaster(RoleMasterRequest request, AllGlobalValues globalValues);
        IEnumerable<RoleMasterModel> RoleMasterList();
    }
}