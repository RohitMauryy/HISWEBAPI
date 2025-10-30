namespace HISWEBAPI.Models
{
  
    public class SMSAPIConfiguration
    {
        public int Id { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string SenderId { get; set; }
        public string NumberPlaceholder { get; set; }
        public string MessagePlaceholder { get; set; }
        public string Format { get; set; }
        public int Timeout { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        /// <summary>
        /// Builds the complete SMS API URL with contact number and message
        /// </summary>
        public string BuildSmsUrl(string contactNumber, string message)
        {
            string url = $"{BaseUrl}?apikey={ApiKey}&senderid={SenderId}&format={Format}";
            url += $"&number={contactNumber}";
            url += $"&message={System.Web.HttpUtility.UrlEncode(message)}";
            return url;
        }
    }
}