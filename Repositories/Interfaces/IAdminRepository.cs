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
        ServiceResult<string> CreateUpdateUserGroupMaster(UserGroupRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserGroupMasterModel>> UserGroupList();
        ServiceResult<string> CreateUpdateUserGroupMembers(UserGroupMembersRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserGroupMembersModel>> UserGroupMembersList(int? groupId);
        ServiceResult<IEnumerable<UserRoleMappingModel>> GetAssignRoleForUserAuthorization(int branchId, int typeId, int userId);
        ServiceResult<string> SaveUpdateRoleMapping(int userId, int branchId, int typeId, List<UserRoleMappingRequest> request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserRightMappingModel>> GetAssignUserRightMapping(int branchId,int typeId,int userId,int roleId);
        ServiceResult<string> SaveUpdateUserRightMapping(SaveUserRightMappingRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<DashboardUserRightMappingModel>> GetAssignDashBoardUserRight(int branchId,int typeId,int userId,int roleId);
        ServiceResult<string> SaveUpdateDashBoardUserRightMapping(SaveDashboardUserRightMappingRequest request, AllGlobalValues globalValues);



    }
}