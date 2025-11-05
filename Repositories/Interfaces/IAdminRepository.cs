using System.Collections.Generic;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        ServiceResult<string> CreateUpdateRoleMaster(RoleMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<RoleMasterModel>> RoleMasterList();
        ServiceResult<IEnumerable<FaIconModel>> getFaIconMaster();
        ServiceResult<UserMasterResponse> CreateUpdateUserMaster(UserMasterRequest request);
        ServiceResult<IEnumerable<UserMasterModel>> UserMasterList();
        ServiceResult<string> CreateUpdateUserDepartment(UserDepartmentRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserDepartmentMasterModel>> UserDepartmentList();

    }
}