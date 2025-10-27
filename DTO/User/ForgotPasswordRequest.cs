using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO.User
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact must be exactly 10 digits")]
        public string Contact { get; set; }
    }

    public class ForgotPasswordResponse
    {
        public bool Result { get; set; }
        public string Message { get; set; }
        public string ContactHint { get; set; }
        public long? UserId { get; set; }
    }
}