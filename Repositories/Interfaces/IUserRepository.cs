using HISWEBAPI.DTO.User;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        long InsertUserMaster(UserMasterRequest request);
        (bool userExists, bool contactMatch, long userId, string registeredContact) ValidateUserForPasswordReset(string userName, string contact);
        (int result, string message) VerifyOtpAndResetPassword(string userName, string otp, string newPassword);
        bool StoreOtpForPasswordReset(long userId, string otp, int expiryMinutes);

    }
}

