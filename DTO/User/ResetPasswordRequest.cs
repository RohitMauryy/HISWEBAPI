using System.ComponentModel.DataAnnotations;
using HISWEBAPI.Attributes;

namespace HISWEBAPI.DTO.User
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [PasswordPolicy]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [PasswordPolicy]
        [Compare("NewPassword", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; }
    }
}
