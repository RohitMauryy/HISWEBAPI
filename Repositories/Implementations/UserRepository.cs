using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.DTO;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;
using HISWEBAPI.Utilities;
using HISWEBAPI.Exceptions;

namespace HISWEBAPI.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;

        public UserRepository(ICustomSqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
        }

        public UserLoginResponse UserLogin(int branchId, string userName, string password)
        {
            DataTable dt = _sqlHelper.GetDataTable("sp_S_Login", CommandType.StoredProcedure, new
            {
                BranchId = branchId,
                UserName = userName
            });

            if (dt == null || dt.Rows.Count == 0)
                return null;

            DataRow userRow = dt.Rows[0];
            string storedHash = Convert.ToString(userRow["Password"]);
            bool isPasswordValid = PasswordHasher.VerifyPassword(password, storedHash);

            if (isPasswordValid)
            {
                return new UserLoginResponse
                {
                    UserId = (int)Convert.ToInt64(userRow["Id"]),
                    Email = Convert.ToString(userRow["Email"]),
                    Contact = Convert.ToString(userRow["Contact"]),
                    IsContactVerified = Convert.ToBoolean(userRow["IsContactVerified"]),
                    IsEmailVerified = Convert.ToBoolean(userRow["IsEmailVerified"])
                };
            }

            return null;
        }
        public long NewUserSignUp(UserSignupRequest request)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Address", request.Address ?? (object)DBNull.Value),
                new SqlParameter("@Contact", request.Contact ?? (object)DBNull.Value),
                new SqlParameter("@DOB", request.DOB != default(DateTime) ? (object)request.DOB : DBNull.Value),
                new SqlParameter("@Email", request.Email ?? (object)DBNull.Value),
                new SqlParameter("@FirstName", request.FirstName),
                new SqlParameter("@MidelName", request.MiddleName ?? (object)DBNull.Value),
                new SqlParameter("@LastName", request.LastName ?? (object)DBNull.Value),
                new SqlParameter("@Password", PasswordHasher.HashPassword(request.Password)),
                new SqlParameter("@UserName", request.UserName),
                new SqlParameter("@Gender", request.Gender ?? (object)DBNull.Value),
                new SqlParameter("@Result", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
            };

            return _sqlHelper.RunProcedureInsert("I_NewUserSignUp", parameters);
        }

        #region SMS & Email OTP Methods

        // ==================== SMS OTP Methods ====================

        public (bool userExists, bool contactMatch, long userId, string registeredContact) ValidateUserForPasswordReset(string userName, string contact)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserName", userName),
                new SqlParameter("@Contact", contact),
                new SqlParameter("@UserExists", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@ContactMatch", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@UserId", SqlDbType.BigInt) { Direction = ParameterDirection.Output },
                new SqlParameter("@RegisteredContact", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_ValidateUserForPasswordReset", parameters);

            bool userExists = parameters[2].Value != DBNull.Value && (bool)parameters[2].Value;
            bool contactMatch = parameters[3].Value != DBNull.Value && (bool)parameters[3].Value;
            long userId = parameters[4].Value != DBNull.Value ? Convert.ToInt64(parameters[4].Value) : 0;
            string registeredContact = parameters[5].Value != DBNull.Value ? parameters[5].Value.ToString() : string.Empty;

            return (userExists, contactMatch, userId, registeredContact);
        }

        public bool StoreOtpForPasswordReset(long userId, string otp, int expiryMinutes)
        {
            try
            {
                var result = _sqlHelper.GetDataTable("sp_StoreOtpForPasswordReset", CommandType.StoredProcedure,
                    new { UserId = userId, Otp = otp, ExpiryMinutes = expiryMinutes });

                if (result != null && result.Rows.Count > 0)
                {
                    int resultValue = Convert.ToInt32(result.Rows[0]["Result"]);
                    return resultValue == 1;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public (int result, string message) VerifySmsOtp(long userId, string otp)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Otp", otp),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("@Message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_VerifySmsOtp", parameters);

            int result = parameters[2].Value != DBNull.Value ? Convert.ToInt32(parameters[2].Value) : 0;
            string message = parameters[3].Value != DBNull.Value ? parameters[3].Value.ToString() : "Unknown error";

            return (result, message);
        }


        // ==================== Common Password Reset Method ====================
        public (bool result, string message) ResetPasswordByUserId(long userId, string otp, string hashedPassword)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@otp", otp),
                    new SqlParameter("@NewPassword", hashedPassword),
                    new SqlParameter("@Result", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new SqlParameter("@Message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
                };

                _sqlHelper.RunProcedure("sp_ResetPasswordByUserId", parameters);

                bool result = parameters[3].Value != DBNull.Value && (bool)parameters[3].Value;
                string message = parameters[4].Value != DBNull.Value ? parameters[4].Value.ToString() : "Unknown error";

                return (result, message);
            }
            catch (Exception ex)
            {
                return (false, "Error occurred while resetting password.");
            }
        }


        // ==================== EMAIL OTP Methods ====================

        public (bool userExists, bool emailMatch, long userId, string registeredEmail) ValidateUserForEmailPasswordReset(string userName, string email)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserName", userName),
                new SqlParameter("@Email", email),
                new SqlParameter("@UserExists", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@EmailMatch", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                new SqlParameter("@UserId", SqlDbType.BigInt) { Direction = ParameterDirection.Output },
                new SqlParameter("@RegisteredEmail", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_ValidateUserForEmailPasswordReset", parameters);

            bool userExists = parameters[2].Value != DBNull.Value && (bool)parameters[2].Value;
            bool emailMatch = parameters[3].Value != DBNull.Value && (bool)parameters[3].Value;
            long userId = parameters[4].Value != DBNull.Value ? Convert.ToInt64(parameters[4].Value) : 0;
            string registeredEmail = parameters[5].Value != DBNull.Value ? parameters[5].Value.ToString() : string.Empty;

            return (userExists, emailMatch, userId, registeredEmail);
        }

        public bool StoreEmailOtpForPasswordReset(long userId, string otp, int expiryMinutes)
        {
            try
            {
                var result = _sqlHelper.GetDataTable("sp_StoreEmailOtpForPasswordReset", CommandType.StoredProcedure,
                    new { UserId = userId, Otp = otp, ExpiryMinutes = expiryMinutes });

                if (result != null && result.Rows.Count > 0)
                {
                    int resultValue = Convert.ToInt32(result.Rows[0]["Result"]);
                    return resultValue == 1;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public (int result, string message) VerifyEmailOtp(long userId, string otp)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Otp", otp),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("@Message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_VerifyEmailOtp", parameters);

            int result = parameters[2].Value != DBNull.Value ? Convert.ToInt32(parameters[2].Value) : 0;
            string message = parameters[3].Value != DBNull.Value ? parameters[3].Value.ToString() : "Unknown error";

            return (result, message);
        }

        #endregion


        public (bool success, string message) UpdateUserPassword(UpdatePasswordRequest model)
        {
            try
            {
                DataTable dt = _sqlHelper.GetDataTable("S_GetUserByUserId", CommandType.StoredProcedure, new
                {
                    @userId = model.UserId
                });

                if (dt == null || dt.Rows.Count == 0)
                    return (false, "User not found.");

                string storedHash = dt.Rows[0]["Password"].ToString();

                bool isValid = PasswordHasher.VerifyPassword(model.CurrentPassword, storedHash);
                if (!isValid)
                    return (false, "Current password not matched.");

                string newHashedPassword = PasswordHasher.HashPassword(model.NewPassword);

                var result = _sqlHelper.DML("U_UserPasswords", CommandType.StoredProcedure, new
                {
                    @userId = model.UserId,
                    @newPassword = newHashedPassword
                });

                if (result < 0)
                    return (false, "Password update failed.");

                return (true, "Password updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Server error occurred.");
            }
        }

        public DataTable GetLoginUserRoles(UserRoleRequest request)
        {
            try
            {
                DataTable dt = _sqlHelper.GetDataTable("S_GetUserRoles", CommandType.StoredProcedure, new
                {
                    @branchId = request.BranchId,
                    @userId = request.UserId
                });

                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Login History & Session Management Methods
        public long CreateLoginSession(LoginSessionRequest request)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@UserId", request.UserId),
                    new SqlParameter("@BranchId", request.BranchId),
                    new SqlParameter("@IpAddress", request.IpAddress ?? (object)DBNull.Value),
                    new SqlParameter("@UserAgent", request.UserAgent ?? (object)DBNull.Value),
                    new SqlParameter("@Browser", request.Browser ?? (object)DBNull.Value),
                    new SqlParameter("@BrowserVersion", request.BrowserVersion ?? (object)DBNull.Value),
                    new SqlParameter("@OperatingSystem", request.OperatingSystem ?? (object)DBNull.Value),
                    new SqlParameter("@Device", request.Device ?? (object)DBNull.Value),
                    new SqlParameter("@DeviceType", request.DeviceType ?? (object)DBNull.Value),
                    new SqlParameter("@Location", request.Location ?? (object)DBNull.Value),
                    new SqlParameter("@SessionId", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
                };

                _sqlHelper.RunProcedure("sp_I_UserLoginSession", parameters);

                return parameters[10].Value != DBNull.Value ? Convert.ToInt64(parameters[10].Value) : 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public bool UpdateLoginSession(long sessionId, string status, string logoutReason = null)
        {
            try
            {
                var result = _sqlHelper.DML("sp_U_UserLoginSession", CommandType.StoredProcedure, new
                {
                    @SessionId = sessionId,
                    @Status = status,
                    @LogoutReason = logoutReason ?? (object)DBNull.Value
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool SaveRefreshToken(long userId, long sessionId, string refreshToken, DateTime expiryDate)
        {
            try
            {
                var result = _sqlHelper.DML("sp_I_UserRefreshToken", CommandType.StoredProcedure, new
                {
                    @UserId = userId,
                    @SessionId = sessionId,
                    @RefreshToken = refreshToken,
                    @ExpiryDate = expiryDate
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public (bool isValid, long sessionId, long userId) ValidateRefreshToken(string refreshToken)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@RefreshToken", refreshToken),
                    new SqlParameter("@IsValid", SqlDbType.Bit) { Direction = ParameterDirection.Output },
                    new SqlParameter("@SessionId", SqlDbType.BigInt) { Direction = ParameterDirection.Output },
                    new SqlParameter("@UserId", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
                };

                _sqlHelper.RunProcedure("sp_ValidateRefreshToken", parameters);

                bool isValid = parameters[1].Value != DBNull.Value && (bool)parameters[1].Value;
                long sessionId = parameters[2].Value != DBNull.Value ? Convert.ToInt64(parameters[2].Value) : 0;
                long userId = parameters[3].Value != DBNull.Value ? Convert.ToInt64(parameters[3].Value) : 0;

                return (isValid, sessionId, userId);
            }
            catch (Exception ex)
            {
                return (false, 0, 0);
            }
        }

        public bool InvalidateRefreshToken(long sessionId)
        {
            try
            {
                var result = _sqlHelper.DML("sp_InvalidateRefreshToken", CommandType.StoredProcedure, new
                {
                    @SessionId = sessionId
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool InvalidateAllUserSessions(long userId)
        {
            try
            {
                var result = _sqlHelper.DML("sp_InvalidateAllUserSessions", CommandType.StoredProcedure, new
                {
                    @UserId = userId
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public DataTable GetUserLoginHistory(long userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                DataTable dt = _sqlHelper.GetDataTable("sp_S_UserLoginHistory", CommandType.StoredProcedure, new
                {
                    @UserId = userId,
                    @PageNumber = pageNumber,
                    @PageSize = pageSize
                });

                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public DataTable GetActiveUserSessions(long userId)
        {
            try
            {
                DataTable dt = _sqlHelper.GetDataTable("sp_S_ActiveUserSessions", CommandType.StoredProcedure, new
                {
                    @UserId = userId
                });

                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}