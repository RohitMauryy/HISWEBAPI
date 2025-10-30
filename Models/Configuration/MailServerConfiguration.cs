namespace HISWEBAPI.Models
{
  
    public class MailServerConfiguration
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSSL { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool IsBodyHtml { get; set; }
        public int Timeout { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }

   
}