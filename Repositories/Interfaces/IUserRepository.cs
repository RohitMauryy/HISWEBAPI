using System.Data;
using HISWEBAPI.DTO.User;

public interface IUserRepository
{
    long UserLogin(int branchId, string userName, string password);
    long InsertUserMaster(UserMasterRequest request);
    (bool userExists, bool contactMatch, long userId, string registeredContact) ValidateUserForPasswordReset(string userName, string contact);
    bool StoreOtpForPasswordReset(long userId, string otp, int expiryMinutes);
    (int result, string message) VerifyOtpAndResetPassword(string userName, string otp, string newPassword);
    (bool success, string message) UpdateUserPassword(UpdatePasswordRequest model);
    DataTable GetLoginUserRoles(UserRoleRequest request);

    long CreateLoginSession(LoginSessionRequest request);
    bool UpdateLoginSession(long sessionId, string status, string logoutReason = null);
    bool SaveRefreshToken(long userId, long sessionId, string refreshToken, DateTime expiryDate);
    (bool isValid, long sessionId, long userId) ValidateRefreshToken(string refreshToken);
    bool InvalidateRefreshToken(long sessionId);
    bool InvalidateAllUserSessions(long userId);
    DataTable GetUserLoginHistory(long userId, int pageNumber = 1, int pageSize = 10);
    DataTable GetActiveUserSessions(long userId);

}