using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO.User
{
    public class SendEmailOtpRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }

}