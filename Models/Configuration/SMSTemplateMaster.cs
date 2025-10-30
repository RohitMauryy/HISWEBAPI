namespace HISWEBAPI.Models
{
    /// <summary>
    /// SMS Template Master Model
    /// </summary>
    public class SMSTemplateMaster
    {
        public int TemplateId { get; set; }
        public int BranchId { get; set; }
        public string Type { get; set; }
        public string SMSTemplate { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? TypeId { get; set; }
        public bool Whatsapp { get; set; }
        public bool SMS { get; set; }
        public bool Email { get; set; }
        public string ForSMSDetails { get; set; }

        /// <summary>
        /// Replace placeholders in template with actual values
        /// </summary>
        /// <param name="replacements">Dictionary of placeholder and their replacement values</param>
        /// <returns>Formatted message</returns>
        public string FormatMessage(Dictionary<string, string> replacements)
        {
            string message = SMSTemplate;

            if (replacements != null)
            {
                foreach (var replacement in replacements)
                {
                    message = message.Replace(replacement.Key, replacement.Value);
                }
            }

            return message;
        }
    }
}