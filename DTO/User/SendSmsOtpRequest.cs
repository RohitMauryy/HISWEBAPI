using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO.User
{
    public class SendSmsOtpRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Contact is required")]
        public string Contact { get; set; }
    }
}
