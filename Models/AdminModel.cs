namespace HISWEBAPI.Models
{
    public class AllGlobalValues
    {
        public int hospId { get; set; }
        public int userId { get; set; }
        public string ipAddress { get; set; }
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
        public string RoleName { get; set; }
        public string MappingBranch { get; set; }
        public int IsActive { get; set; }
    }
}
