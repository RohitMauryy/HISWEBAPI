using System.Data;
using HISWEBAPI.DTO;

public interface IUserRepository
{
    UserLoginResponse UserLogin(int branchId, string userName, string password);
    long NewUserSignUp(UserSignupRequest request);

    // SMS OTP Methods
    (bool userExists, bool contactMatch, long userId, string registeredContact) ValidateUserForPasswordReset(string userName, string contact);
    bool StoreOtpForPasswordReset(long userId, string otp, int expiryMinutes);
    (int result, string message) VerifySmsOtp(long userId, string otp);

    // Email OTP Methods
    (bool userExists, bool emailMatch, long userId, string registeredEmail) ValidateUserForEmailPasswordReset(string userName, string email);
    bool StoreEmailOtpForPasswordReset(long userId, string otp, int expiryMinutes);
    (int result, string message) VerifyEmailOtp(long userId, string otp);

    // Common Password Reset
    (bool result, string message) ResetPasswordByUserId(long userId, string otp, string hashedPassword);

    (bool success, string message) UpdateUserPassword(UpdatePasswordRequest model);
    DataTable GetLoginUserRoles(UserRoleRequest request);

    // Session Management
    long CreateLoginSession(LoginSessionRequest request);
    bool UpdateLoginSession(long sessionId, string status, string logoutReason = null);
    bool SaveRefreshToken(long userId, long sessionId, string refreshToken, DateTime expiryDate);
    (bool isValid, long sessionId, long userId) ValidateRefreshToken(string refreshToken);
    bool InvalidateRefreshToken(long sessionId);
    bool InvalidateAllUserSessions(long userId);
    DataTable GetUserLoginHistory(long userId, int pageNumber = 1, int pageSize = 10);
    DataTable GetActiveUserSessions(long userId);
}