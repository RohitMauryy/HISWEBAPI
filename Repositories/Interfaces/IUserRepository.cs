using System.Collections.Generic;
using HISWEBAPI.Models;
using HISWEBAPI.DTO;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        ServiceResult<UserLoginResponseData> UserLogin(UserLoginRequest request);
        ServiceResult<TokenResponseData> RefreshToken(RefreshTokenRequest request);
        ServiceResult<string> Logout(LogoutRequest request);
        ServiceResult<UserSignupResponseData> NewUserSignUp(UserSignupRequest request);
        ServiceResult<SmsOtpResponseData> SendSmsOtp(SendSmsOtpRequest request);
        ServiceResult<OtpVerificationResponseData> VerifySmsOtp(VerifySmsOtpRequest request);
        ServiceResult<EmailOtpResponseData> SendEmailOtp(SendEmailOtpRequest request);
        ServiceResult<string> VerifyEmailOtp(VerifyEmailOtpRequest request);
        ServiceResult<string> ResetPasswordByUserId(ResetPasswordRequest request);
        ServiceResult<string> UpdatePassword(UpdatePasswordRequest request);
        ServiceResult<IEnumerable<UserRoleModel>> GetUserRoles(UserRoleRequest request);
    }
}