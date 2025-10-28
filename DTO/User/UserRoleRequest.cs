using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO.User
{
    public class UserRoleRequest
    {
        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }
    }
}
