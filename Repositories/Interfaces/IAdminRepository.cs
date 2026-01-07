using System.Collections.Generic;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        ServiceResult<string> CreateUpdateRoleMaster(RoleMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<string> UpdateRoleMasterStatus(int roleId, int isActive, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<RoleMasterModel>> RoleMasterList(int? roleId = null);
        ServiceResult<IEnumerable<FaIconModel>> getFaIconMaster();
        ServiceResult<UserMasterResponse> CreateUpdateUserMaster(UserMasterRequest request);
        ServiceResult<string> UpdateUserMasterStatus(int userId, int isActive, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserMasterModel>> UserMasterList(int? userId = null);
        ServiceResult<string> CreateUpdateUserDepartment(UserDepartmentRequest request, AllGlobalValues globalValues);
        ServiceResult<string> UpdateUserDepartmentStatus(int id, int isActive, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserDepartmentMasterModel>> UserDepartmentList(int? id = null);
        ServiceResult<string> CreateUpdateUserGroupMaster(UserGroupRequest request, AllGlobalValues globalValues);
        ServiceResult<string> UpdateUserGroupStatus(int id, int isActive, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserGroupMasterModel>> UserGroupList(int? id = null);
        ServiceResult<string> CreateUpdateUserGroupMembers(UserGroupMembersRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserGroupMembersModel>> UserGroupMembersList(int? groupId);
        ServiceResult<IEnumerable<UserRoleMappingModel>> GetAssignRoleForUserAuthorization(int branchId, int typeId, int userId);
        ServiceResult<string> SaveUpdateRoleMapping(int userId, int branchId, int typeId, List<UserRoleMappingRequest> request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserRightMappingModel>> GetAssignUserRightMapping(int branchId,int typeId,int userId,int roleId);
        ServiceResult<string> SaveUpdateUserRightMapping(SaveUserRightMappingRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<DashboardUserRightMappingModel>> GetAssignDashBoardUserRight(int branchId,int typeId,int userId,int roleId);
        ServiceResult<string> SaveUpdateDashBoardUserRightMapping(SaveDashboardUserRightMappingRequest request, AllGlobalValues globalValues);
        ServiceResult<NavigationTabMasterResponse> CreateUpdateNavigationTabMaster(NavigationTabMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<NavigationTabMasterModel>> GetNavigationTabMaster();
        ServiceResult<NavigationSubMenuMasterResponse> CreateUpdateNavigationSubMenuMaster(NavigationSubMenuMasterRequest request,AllGlobalValues globalValues);
        ServiceResult<IEnumerable<NavigationSubMenuMasterModel>> GetNavigationSubMenuMaster();
        ServiceResult<string> SaveUpdateRoleWiseMenuMapping(SaveRoleWiseMenuMappingRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<RoleWiseMenuMappingModel>> GetRoleWiseMenuMapping(int branchId, int roleId);
        ServiceResult<string> SaveUpdateUserMenuMaster(SaveUserMenuMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserWiseMenuMasterModel>> GetUserWiseMenuMaster(int branchId, int typeId, int userId, int roleId);
        ServiceResult<string> SaveUpdateUserCorporateMapping(SaveUserCorporateMappingRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserWiseCorporateMappingModel>> GetUserWiseCorporateMapping(int branchId, int typeId, int userId);
        ServiceResult<string> SaveUpdateUserBedMapping(SaveUserBedMappingRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<UserWiseBedMappingModel>> GetUserWiseBedMapping(int branchId, int typeId, int userId);
        ServiceResult<BranchMasterResponse> CreateUpdateBranchMaster(BranchMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<IEnumerable<BranchMasterModel>> GetBranchDetails(int? branchId = null);
        ServiceResult<int> CreateUpdateStateMaster(CreateUpdateStateMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<int> CreateUpdateDistrictMaster(CreateUpdateDistrictMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<int> CreateUpdateCityMaster(CreateUpdateCityMasterRequest request, AllGlobalValues globalValues);
        ServiceResult<int> CreateUpdatePincodeMaster(CreateUpdatePincodeMasterRequest request, AllGlobalValues globalValues);



    }
}