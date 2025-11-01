namespace HISWEBAPI.Models
{
    public class ResponseMessageModel
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public string Type { get; set; }
        public string AlertCode { get; set; }
        public string Message { get; set; }
    }

    public class ResponseMessage
    {
        public string AlertCode { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }

    
}