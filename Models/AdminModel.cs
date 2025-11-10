namespace HISWEBAPI.Models
{
    public class AllGlobalValues
    {
        public int hospId { get; set; }
        public int userId { get; set; }
        public int branchId { get; set; }
        public string? ipAddress { get; set; }
    }
    public class BranchModel
    {
        public required int branchId { get; set; }
        public required string branchName { get; set; }
    }

 public class PickListModel
    {
        public required int id { get; set; }
        public required string fieldName { get; set; }
        public required string value { get; set; }
        public required string key { get; set; }
    }


    public class RoleMasterModel
    {
        public int RoleId { get; set; }
        public required string RoleName { get; set; }
        public int FaIconId { get; set; }
        public int IsActive { get; set; }
        public string? IconClass { get; set; }
        public string? IconName { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? LastModifiedOn { get; set; }
    }

    public class FaIconModel
    {
        public int Id { get; set; }
        public string? IconClass { get; set; }
        public string? IconName { get; set; }
      
    }

  

    public class UserMasterModel
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? MidelName { get; set; }
        public string? LastName { get; set; }
        public string? DOB { get; set; }
        public string? Gender { get; set; }
        public required string UserName { get; set; }
        public string? Password { get; set; }
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public string? Email { get; set; }
        public int IsActive { get; set; }
        public string? EmployeeID { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? LastModifiedOn { get; set; }
        public int? ReportToUserId { get; set; }
        public int? UserDepartmentId { get; set; }
    }


    public class UserDepartmentMasterModel
    {
        public int Id { get; set; }
        public required string DepartmentName { get; set; }
        public int IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? LastModifiedOn { get; set; }
        public string? IPAddress { get; set; }
    }

    public class UserGroupMasterModel
    {
        public int Id { get; set; }
        public required string GroupName { get; set; }
        public int IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? LastModifiedOn { get; set; }
        public string? IPAddress { get; set; }
    }

    public class UserGroupMembersModel
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string? GroupName { get; set; }
        public string? UserName { get; set; }
        public int IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? LastModifiedOn { get; set; }
        public string? IPAddress { get; set; }
    }

    public class UserRoleMappingModel
    {
        public int isGranted { get; set; }
        public required string RoleName { get; set; }
        public int RoleId { get; set; }
    }


    public class UserRightMappingModel
    {
        public int IsGranted { get; set; }
        public required string UserRightName { get; set; }
        public string? Description { get; set; }
        public int UserRightId { get; set; }
    }

    public class DashboardUserRightMappingModel
    {
        public int IsGranted { get; set; }
        public required string UserRightName { get; set; }
        public string? Details { get; set; }
        public int UserRightId { get; set; }
    }

   

   
}
