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

        [Required(ErrorMessage = "Image Path is required")]
        [StringLength(256, ErrorMessage = "Image Path cannot exceed 256 characters")]
        public string ImagePath { get; set; }
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

        [Required(ErrorMessage = "IsFirst is required")]
        public int IsFirst { get; set; }

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

    public class UserCorporateMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "CorporateId is required")]
        public int CorporateId { get; set; }
    }

    public class SaveUserCorporateMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "IsFirst is required")]
        public int IsFirst { get; set; }

        public List<UserCorporateMappingRequest> UserCorporates { get; set; } = new List<UserCorporateMappingRequest>();
    }

    public class GetUserWiseCorporateMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }
    }

    public class UserBedMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "ServiceItemId is required")]
        public int ServiceItemId { get; set; }
    }

    public class SaveUserBedMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "IsFirst is required")]
        public int IsFirst { get; set; }

        public List<UserBedMappingRequest> UserBeds { get; set; } = new List<UserBedMappingRequest>();
    }

    public class GetUserWiseBedMappingRequest
    {
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }
    }

    public class BranchMasterRequest
    {
        public int BranchId { get; set; } = 0;

        [Required(ErrorMessage = "Branch name is required")]
        [StringLength(256, ErrorMessage = "Branch name cannot exceed 256 characters")]
        public string BranchName { get; set; }

        [Required(ErrorMessage = "Branch code is required")]
        [StringLength(10, ErrorMessage = "Branch code cannot exceed 10 characters")]
        public string BranchCode { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact number 1 is required")]
        [StringLength(15)]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact must be exactly 10 digits")]
        public string ContactNo1 { get; set; }

        [StringLength(15)]
        public string ContactNo2 { get; set; }

        public string Address { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }

        [Required(ErrorMessage = "Financial year start is required")]
        [StringLength(20)]
        public string FYStartFrom { get; set; }

        [Required(ErrorMessage = "Default country is required")]
        public int DefaultCountryId { get; set; }

        [Required(ErrorMessage = "Default state is required")]
        public int DefaultStateId { get; set; }

        [Required(ErrorMessage = "Default district is required")]
        public int DefaultDistrictId { get; set; }

        [Required(ErrorMessage = "Default city is required")]
        public int DefaultCityId { get; set; }

        public int DefaultInsuranceCompanyId { get; set; }

        public int DefaultCorporateId { get; set; }
    }

    public class BranchMasterResponse
    {
        public int BranchId { get; set; }
    }

    public class CreateUpdateStateMasterRequest
    {
        public int StateId { get; set; } = 0; // 0 or empty = create, >0 = update

        [Required(ErrorMessage = "CountryId is required")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "StateName is required")]
        [StringLength(100, ErrorMessage = "StateName cannot exceed 100 characters")]
        public string StateName { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    // Create/Update District Master Request
    public class CreateUpdateDistrictMasterRequest
    {
        public int DistrictId { get; set; } = 0; // 0 or empty = create, >0 = update

        [Required(ErrorMessage = "StateId is required")]
        public int StateId { get; set; }

        [Required(ErrorMessage = "CountryId is required")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "DistrictName is required")]
        [StringLength(100, ErrorMessage = "DistrictName cannot exceed 100 characters")]
        public string DistrictName { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    // Create/Update City Master Request
    public class CreateUpdateCityMasterRequest
    {
        public int CityId { get; set; } = 0; // 0 or empty = create, >0 = update

        [Required(ErrorMessage = "DistrictId is required")]
        public int DistrictId { get; set; }

        [Required(ErrorMessage = "StateId is required")]
        public int StateId { get; set; }

        [Required(ErrorMessage = "CountryId is required")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "CityName is required")]
        [StringLength(100, ErrorMessage = "CityName cannot exceed 100 characters")]
        public string CityName { get; set; }


        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class CreateUpdatePincodeMasterRequest
    {
        public int PincodeId { get; set; } = 0; // 0 = create, >0 = update

        [Required(ErrorMessage = "CityId is required")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "Pincode is required")]
        [Range(100000, 999999, ErrorMessage = "Pincode must be exactly 6 digits")]
        public int Pincode { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class HeaderMasterRequest
    {
        public int HeaderId { get; set; } = 0;

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Type is required")]
        [StringLength(256, ErrorMessage = "Type cannot exceed 256 characters")]
        public string Type { get; set; }

        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "IsHeader is required")]
        public int IsHeader { get; set; }

        public string HeaderBody { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class HeaderMasterResponse
    {
        public int HeaderId { get; set; }
    }

    public class GetHeaderMasterRequest
    {
        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "IsHeader is required")]
        public int IsHeader { get; set; }
    }

    public class CreateUpdateSequenceMasterRequest
    {
        [Required(ErrorMessage = "SequenceId is required")]
        public int SequenceId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "TypeName is required")]
        [StringLength(100, ErrorMessage = "TypeName cannot exceed 100 characters")]
        public string TypeName { get; set; }

        // Prefix can be blank (empty string), not required
        [StringLength(10, ErrorMessage = "Prefix cannot exceed 10 characters")]
        public string Prefix { get; set; } = string.Empty;

        // FirstSeprator can be blank (empty string), not required
        [StringLength(2, ErrorMessage = "FirstSeprator cannot exceed 2 characters")]
        public string FirstSeprator { get; set; } = string.Empty;

        // FYFormatId can be 0 (no format selected), not required to be > 0
        public int FYFormatId { get; set; } = 0;

        // FYFormat can be blank (empty string), not required
        [StringLength(20, ErrorMessage = "FYFormat cannot exceed 20 characters")]
        public string FYFormat { get; set; } = string.Empty;

        // SecondSeprator can be blank (empty string), not required
        [StringLength(2, ErrorMessage = "SecondSeprator cannot exceed 2 characters")]
        public string SecondSeprator { get; set; } = string.Empty;

        // Length must be greater than 0
        [Required(ErrorMessage = "Length is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Length must be greater than 0")]
        public int Length { get; set; }

        [Required(ErrorMessage = "Preview is required")]
        [StringLength(50, ErrorMessage = "Preview cannot exceed 50 characters")]
        public string Preview { get; set; }
    }

    public class CreateUpdateSequenceMasterResponse
    {
        public int SequenceId { get; set; }
    }

    public class CreateUpdateBranchSequenceMappingRequest
    {
        public int MappingId { get; set; } = 0;

        [Required(ErrorMessage = "BranchId is required")]
        [Range(0, int.MaxValue, ErrorMessage = "BranchId must be greater than or equal to 0")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        [Range(0, int.MaxValue, ErrorMessage = "RoleId must be greater than or equal to 0")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "TypeId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "TypeId must be greater than 0")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "SequenceId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "SequenceId must be greater than 0")]
        public int SequenceId { get; set; }
    }

    public class CreateUpdateBranchSequenceMappingResponse
    {
        public int MappingId { get; set; }
    }

    public class LabReportLetterHeadRequest
    {
        public int Id { get; set; } = 0;

        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }

        [Required(ErrorMessage = "TypeName is required")]
        [StringLength(100, ErrorMessage = "TypeName cannot exceed 100 characters")]
        public string TypeName { get; set; }

        [Range(0, 500, ErrorMessage = "PaddingLeft must be between 0 and 500")]
        public int PaddingLeft { get; set; } = 0;

        [Range(0, 500, ErrorMessage = "PaddingRight must be between 0 and 500")]
        public int PaddingRight { get; set; } = 0;

        [Range(0, 500, ErrorMessage = "PaddingTop must be between 0 and 500")]
        public int PaddingTop { get; set; } = 0;

        [Range(0, 500, ErrorMessage = "PaddingBottom must be between 0 and 500")]
        public int PaddingBottom { get; set; } = 0;

        public IFormFile? LetterHeadFile { get; set; }

      
    }

   
    public class LabReportLetterHeadResponse
    {
        public int Id { get; set; }
        public string LetterHeadFilePath { get; set; }
    }
    public class DeleteLetterHeadRequest
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; }
    }

    public class DoctorSignatureMasterRequest
    {
        public int Id { get; set; } = 0;

        [Required(ErrorMessage = "BranchId is required")]
        [Range(0, int.MaxValue, ErrorMessage = "BranchId must be greater than or equal to 0")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "DoctorId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "DoctorId must be greater than 0")]
        public int DoctorId { get; set; }

        [Range(0, 1000, ErrorMessage = "XSign must be between 0 and 1000")]
        public int XSign { get; set; } = 0;

        [Range(0, 1000, ErrorMessage = "YSign must be between 0 and 1000")]
        public int YSign { get; set; } = 0;

        public IFormFile? DocSignFile { get; set; }
    }

    public class DoctorSignatureMasterResponse
    {
        public int Id { get; set; }
        public string DocSignPath { get; set; }
    }

    public class DeleteDoctorSignatureRequest
    {
        [Required(ErrorMessage = "Id is required")]
        public int Id { get; set; }
    }

    public class BankMasterRequest
    {
        public int BankId { get; set; } = 0;

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(256, ErrorMessage = "Bank name cannot exceed 256 characters")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class BankMasterResponse
    {
        public int BankId { get; set; }
    }

    public class BankDetailMasterRequest
    {
        public int BankId { get; set; } = 0;

        [Required(ErrorMessage = "Payee name is required")]
        [StringLength(256, ErrorMessage = "Payee name cannot exceed 256 characters")]
        public string PayeeName { get; set; }

        [Required(ErrorMessage = "PAN number is required")]
        [StringLength(20, ErrorMessage = "PAN number cannot exceed 20 characters")]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Invalid PAN number format")]
        public string PANNumber { get; set; }

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(256, ErrorMessage = "Bank name cannot exceed 256 characters")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "Bank account number is required")]
        [StringLength(20, ErrorMessage = "Bank account number cannot exceed 20 characters")]
        public string BankAccountNumber { get; set; }

        [Required(ErrorMessage = "Bank address is required")]
        [StringLength(256, ErrorMessage = "Bank address cannot exceed 256 characters")]
        public string BankAddress { get; set; }

        [Required(ErrorMessage = "IFSC code is required")]
        [StringLength(100, ErrorMessage = "IFSC code cannot exceed 100 characters")]
        [RegularExpression(@"^[A-Z]{4}0[A-Z0-9]{6}$", ErrorMessage = "Invalid IFSC code format")]
        public string IFSCCode { get; set; }

        [Required(ErrorMessage = "PIN code is required")]
        [StringLength(10, ErrorMessage = "PIN code cannot exceed 10 characters")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "PIN code must be 6 digits")]
        public string PINCode { get; set; }

        [Required(ErrorMessage = "TIN number is required")]
        [StringLength(20, ErrorMessage = "TIN number cannot exceed 20 characters")]
        public string TINNumber { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class BankDetailMasterResponse
    {
        public int BankId { get; set; }
    }

    // MRD Room Master DTOs
    public class MRDRoomMasterRequest
    {
        public int RoomId { get; set; } = 0;

        [Required(ErrorMessage = "Room name is required")]
        [StringLength(256, ErrorMessage = "Room name cannot exceed 256 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class MRDRoomMasterResponse
    {
        public int RoomId { get; set; }
    }

    // MRD Rack Master DTOs
    public class MRDRackMasterRequest
    {
        public int RackId { get; set; } = 0;

        [Required(ErrorMessage = "RoomId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "RoomId must be greater than 0")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Rack name is required")]
        [StringLength(256, ErrorMessage = "Rack name cannot exceed 256 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }

        [Range(0, 100, ErrorMessage = "AutoCreateShelfs must be between 0 and 100")]
        public int AutoCreateShelfs { get; set; } = 0;
    }

    public class MRDRackMasterResponse
    {
        public int RackId { get; set; }
    }

    // MRD Shelf Master DTOs
    public class MRDShelfMasterRequest
    {
        public int ShelfId { get; set; } = 0;

        [Required(ErrorMessage = "RoomId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "RoomId must be greater than 0")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "RackId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "RackId must be greater than 0")]
        public int RackId { get; set; }

        [Required(ErrorMessage = "Shelf name is required")]
        [StringLength(256, ErrorMessage = "Shelf name cannot exceed 256 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class MRDShelfMasterResponse
    {
        public int ShelfId { get; set; }
    }

    public class PatientDocumentMasterRequest
    {
        public int DocumentId { get; set; } = 0;

        [Required(ErrorMessage = "Document name is required")]
        [StringLength(256, ErrorMessage = "Document name cannot exceed 256 characters")]
        public string DocumentName { get; set; }

        [Required(ErrorMessage = "Document code is required")]
        [StringLength(20, ErrorMessage = "Document code cannot exceed 20 characters")]
        public string DocumentCode { get; set; }

        [Required(ErrorMessage = "IsActive is required")]
        public int IsActive { get; set; }
    }

    public class PatientDocumentMasterResponse
    {
        public int DocumentId { get; set; }
    }
}
