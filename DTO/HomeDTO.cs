using System.ComponentModel.DataAnnotations;

namespace HISWEBAPI.DTO
{
    public class ResponseMessageRequest
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "TypeId is required")]
        public int TypeId { get; set; }
        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; }
        [Required(ErrorMessage = "AlertCode is required")]
        public string AlertCode { get; set; }
        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; }
        public bool IsActive { get; set; }
    }
}
