using System.Collections.Generic;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        ServiceResult<string> CreateUpdateRoleMaster(RoleMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<RoleMasterModel>> RoleMasterList();
    }
}