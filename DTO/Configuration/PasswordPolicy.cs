namespace HISWEBAPI.DTO.Configuration
{
    public class PasswordPolicy
    {
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public string Regex { get; set; }
        public string ErrorMessage { get; set; }
    }
}
