namespace HISWEBAPI.Models
{
    public class AllGlobalValues
    {
        public int hospId { get; set; }
        public int userId { get; set; }
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
        public long userId { get; set; }
    }
}
