using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO.User
{
    public class VerifyEmailOtpRequest
    {

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; }
        public int UserId { get; set; }

    }
}
