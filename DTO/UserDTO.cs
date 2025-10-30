using System.ComponentModel.DataAnnotations;
using HISWEBAPI.Attributes;

namespace HISWEBAPI.DTO
{
    public class SendEmailOtpRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }

    public class LoginSessionRequest
    {
        public long UserId { get; set; }
        public int BranchId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Browser { get; set; }
        public string BrowserVersion { get; set; }
        public string OperatingSystem { get; set; }
        public string Device { get; set; }
        public string DeviceType { get; set; }
        public string Location { get; set; }
    }

    public class LoginHistoryResponse
    {
        public long SessionId { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public string IpAddress { get; set; }
        public string Browser { get; set; }
        public string OperatingSystem { get; set; }
        public string Device { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public string LogoutReason { get; set; }
    }

    public class ActiveSessionResponse
    {
        public long SessionId { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public string IpAddress { get; set; }
        public string Browser { get; set; }
        public string OperatingSystem { get; set; }
        public string Device { get; set; }
        public bool IsCurrentSession { get; set; }
    }

    public class LogoutRequest
    {
        public long SessionId { get; set; }
        public string LogoutReason { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [PasswordPolicy]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [PasswordPolicy]
        [Compare("NewPassword", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; }
    }

    public class SendSmsOtpRequest
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Contact is required")]
        public string Contact { get; set; }
    }

    public class TerminateSessionRequest
    {
        public long SessionId { get; set; }
    }

    public class UpdatePasswordRequest
    {
        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Current Password is required")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmNewPassword { get; set; }
    }

    public class UserLoginRequest
    {
        [Required(ErrorMessage = "Branch Id is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Access token is required")]
        public string AccessToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UserLoginResponse
    {
        public int UserId { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public bool IsContactVerified { get; set; }
        public bool IsEmailVerified { get; set; }
    }

    public class UserRoleRequest
    {
        [Required(ErrorMessage = "BranchId is required")]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }
    }

    public class UserSignupRequest
    {

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string MiddleName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [PasswordPolicy]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Contact must be exactly 10 digits")]
        public string Contact { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DOB { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
        public string Gender { get; set; }

    }

    public class VerifyEmailOtpRequest
    {

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; }
        public int UserId { get; set; }

    }

    public class VerifySmsOtpRequest
    {
        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public int UserId { get; set; }
    }

}
