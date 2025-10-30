using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO
{

   
    public class RoleMasterRequest
    {
        public int RoleId { get; set; } = 0;

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(256, ErrorMessage = "Role name cannot exceed 256 characters")]
        public string RoleName { get; set; }

        [Required(ErrorMessage = "IsActive status is required")]
        public int IsActive { get; set; }

        [StringLength(100, ErrorMessage = "Mapping branch cannot exceed 100 characters")]
        public string MappingBranch { get; set; }
    }
}
