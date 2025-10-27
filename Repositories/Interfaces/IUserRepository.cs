using HISWEBAPI.DTO.User;

public interface IUserRepository
{
    long InsertUserMaster(UserMasterRequest request);
    (bool userExists, bool contactMatch, long userId, string registeredContact) ValidateUserForPasswordReset(string userName, string contact);
    bool StoreOtpForPasswordReset(long userId, string otp, int expiryMinutes);
    (int result, string message) VerifyOtpAndResetPassword(string userName, string otp, string newPassword);
}