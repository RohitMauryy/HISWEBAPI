using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO.User
{
    public class VerifySmsOtpRequest
    {
        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }
    }
}
