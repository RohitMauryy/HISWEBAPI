using System.ComponentModel.DataAnnotations;
using HISWEBAPI.Attributes;

namespace HISWEBAPI.DTO
{
    public class PageConfigRequest
    {
       
        public int Id { get; set; } = 0;

        [Required(ErrorMessage = "ConfigKey is required")]
        [StringLength(256, ErrorMessage = "ConfigKey cannot exceed 256 characters")]
        public string ConfigKey { get; set; }

        [Required(ErrorMessage = "ConfigJson is required")]
        public string ConfigJson { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class PageConfigResponse
    {
        public int Id { get; set; }
        public string ConfigKey { get; set; }
        public string ConfigJson { get; set; }
       
    }

    
    public class GetPageConfigRequest
    {
        [StringLength(256, ErrorMessage = "ConfigKey cannot exceed 256 characters")]
        public string ConfigKey { get; set; }
    }

    public class RoleMasterRequest
    {
        public int RoleId { get; set; } = 0;

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(256, ErrorMessage = "Role name cannot exceed 256 characters")]
        public string RoleName { get; set; }

        [Required(ErrorMessage = "IsActive status is required")]
        public int IsActive { get; set; }

        [Required(ErrorMessage = "FaIconId is required")]
        public int FaIconId { get; set; } = 0;
    }

    public class UserMasterRequest
    {
        public int userId { get; set; } = 0;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string MiddleName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [PasswordPolicy]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact must be exactly 10 digits")]
        public string Contact { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
        public string Gender { get; set; }
        public int IsActive { get; set; }
        public string EmployeeID { get; set; }
        public int ReportToUserId { get; set; }
        public int UserDepartmentId { get; set; }

    }

    public class UserMasterResponse
    {
        public long userId { get; set; }
    }

    public class UserDepartmentRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department name is required")]
        [StringLength(200, ErrorMessage = "Department name cannot exceed 200 characters")]
        public required string DepartmentName { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class UserGroupRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Group name is required")]
        [StringLength(200, ErrorMessage = "Group name cannot exceed 200 characters")]
        public required string GroupName { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class UserGroupMembersRequest
    {
        [Required(ErrorMessage = "GroupId is required")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "UserIds are required")]
        [MinLength(1, ErrorMessage = "At least one user must be selected")]
        public required List<int> UserIds { get; set; }
    }

    public class UserRoleMappingRequest
    {
        [Required(ErrorMessage = "UserId is required")]
        public int userId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int branchId { get; set; }

        [Required(ErrorMessage = "TypeId is required")]
        public int typeId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int roleId { get; set; }
    }

    public class UserRoleMappingListRequest
    {
        [Required(ErrorMessage = "UserId is required")]
        public int userId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int branchId { get; set; }

        [Required(ErrorMessage = "TypeId is required")]
        public int typeId { get; set; }

        public List<UserRoleMappingRequest>? userRoleMappings { get; set; }
    }


    public class UserRightsRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "UserRightId is required")]
        public int UserRightId { get; set; }
    }

    public class SaveUserRightMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        public List<UserRightsRequest> UserRights { get; set; } = new List<UserRightsRequest>();
    }


    public class DashboardUserRightsRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "UserRightId is required")]
        public int UserRightId { get; set; }
    }

    public class SaveDashboardUserRightMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        public List<DashboardUserRightsRequest> DashboardUserRights { get; set; } = new List<DashboardUserRightsRequest>();
    }

    public class NavigationTabMasterRequest
    {
        public int TabId { get; set; } = 0;

        [Required(ErrorMessage = "Tab name is required")]
        [StringLength(100, ErrorMessage = "Tab name cannot exceed 100 characters")]
        public string TabName { get; set; }

        [Required(ErrorMessage = "FaIconId is required")]
        public int FaIconId { get; set; }
    }
    public class NavigationTabMasterResponse
    {
        public int TabId { get; set; }
    }



    public class NavigationSubMenuMasterRequest
    {
        public int SubMenuId { get; set; } = 0;

        [Required(ErrorMessage = "TabId is required")]
        public int TabId { get; set; }

        [Required(ErrorMessage = "Sub menu name is required")]
        [StringLength(512, ErrorMessage = "Sub menu name cannot exceed 512 characters")]
        public string SubMenuName { get; set; }

        [Required(ErrorMessage = "URL is required")]
        public string URL { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class NavigationSubMenuMasterResponse
    {
        public int SubMenuId { get; set; }
    }



    public class RoleWiseMenuMappingRequest
    {
        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "SubMenuId is required")]
        public int SubMenuId { get; set; }
    }

    public class SaveRoleWiseMenuMappingRequest
    {
      

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }
        public List<RoleWiseMenuMappingRequest> MenuMappings { get; set; } = new List<RoleWiseMenuMappingRequest>();
    }

    public class GetRoleWiseMenuMappingRequest
    {
        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }
       
        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }
    }


    public class UserMenuMasterRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "SubMenuId is required")]
        public int SubMenuId { get; set; }
    }

    public class SaveUserMenuMasterRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "IsFirst is required")]
        public int IsFirst { get; set; }

        public List<UserMenuMasterRequest> UserMenus { get; set; } = new List<UserMenuMasterRequest>();
    }

    public class GetUserWiseMenuMasterRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }
    }

}
