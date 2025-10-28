using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.DTO.User;
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

        public long UserLogin(int branchId, string userName, string password)
        {
            DataTable dt = _sqlHelper.GetDataTable("sp_S_Login", CommandType.StoredProcedure, new
            {
                BranchId = branchId,
                UserName = userName
            });

            if (dt == null || dt.Rows.Count == 0)
                return 0;

            DataRow userRow = dt.Rows[0];
            string storedHash = Convert.ToString(userRow["Password"]);

            bool isPasswordValid = PasswordHasher.VerifyPassword(password, storedHash);

            if (isPasswordValid)
                return Convert.ToInt64(userRow["Id"]);

            return 0;
        }

        public long InsertUserMaster(UserMasterRequest request)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@HospId", 1),
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
                new SqlParameter("@UserId", request.UserId.HasValue && request.UserId.Value > 0
                    ? (object)request.UserId.Value
                    : DBNull.Value),
                new SqlParameter("@IsActive", request.IsActive),
                new SqlParameter("@EmployeeID", request.EmployeeID ?? (object)DBNull.Value),
                new SqlParameter("@Result", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
            };

            return _sqlHelper.RunProcedureInsert("sp_I_UserMaster", parameters);
        }

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

        public (int result, string message) VerifyOtpAndResetPassword(string userName, string otp, string newPassword)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserName", userName),
                new SqlParameter("@Otp", otp),
                new SqlParameter("@NewPassword", newPassword),
                new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output },
                new SqlParameter("@Message", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output }
            };

            _sqlHelper.RunProcedure("sp_VerifyOtpAndResetPassword", parameters);

            int result = parameters[3].Value != DBNull.Value ? Convert.ToInt32(parameters[3].Value) : 0;
            string message = parameters[4].Value != DBNull.Value ? parameters[4].Value.ToString() : "Unknown error";

            return (result, message);
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