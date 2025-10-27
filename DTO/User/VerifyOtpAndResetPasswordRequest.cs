using System.ComponentModel.DataAnnotations;
using HISWEBAPI.Attributes;

namespace HISWEBAPI.DTO.User
{
    public class VerifyOtpAndResetPasswordRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [RegularExpression(@"^\d{4,6}$", ErrorMessage = "OTP must be 4-6 digits")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [PasswordPolicy]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; }
    }

    public class VerifyOtpAndResetPasswordResponse
    {
        public bool Result { get; set; }
        public string Message { get; set; }
    }
}