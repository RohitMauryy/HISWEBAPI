using System.ComponentModel.DataAnnotations;
using HISWEBAPI.Attributes;

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
        public int ReportToUserId { get; set; }
        public int UserDepartmentId { get; set; }

    }
}
