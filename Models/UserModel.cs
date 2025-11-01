namespace HISWEBAPI.Models
{
    public class UserRoleModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class UserLoginResponseData
    {
        public int userId { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public string contact { get; set; }
        public bool isContactVerified { get; set; }
        public bool isEmailVerified { get; set; }
        public int branchId { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public string tokenType { get; set; }
        public int expiresIn { get; set; }
    }

    public class TokenResponseData
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public string tokenType { get; set; }
        public int expiresIn { get; set; }
    }

    public class UserSignupResponseData
    {
        public long userId { get; set; }
    }

    public class SmsOtpResponseData
    {
        public long userId { get; set; }
        public string contactHint { get; set; }
    }

    public class EmailOtpResponseData
    {
        public long userId { get; set; }
        public string emailHint { get; set; }
    }

    public class OtpVerificationResponseData
    {
        public int userId { get; set; }
        public string otp { get; set; }
    }
}