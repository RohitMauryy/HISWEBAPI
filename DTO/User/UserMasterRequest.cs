using System;
using System.ComponentModel.DataAnnotations;
namespace HISWEBAPI.DTO.User
{
    public class UserMasterRequest
    {
        public long? UserId { get; set; }
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
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{6,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one digit, one special character, and be at least 6 characters long")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; }
        [Required(ErrorMessage = "Contact is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact must be exactly 10 digits")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Contact must be exactly 10 digits")]
        public string Contact { get; set; }
        [StringLength(250)]
        public string Address { get; set; }
        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DOB { get; set; }
        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
        public string Gender { get; set; }
        public string EmployeeID { get; set; }
        public bool IsActive { get; set; } = true;
    }
}