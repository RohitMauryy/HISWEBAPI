using System.Data;
using Microsoft.Data.SqlClient;
using HISWEBAPI.DTO.User;
using HISWEBAPI.Repositories.Interfaces;
using HISWEBAPI.Data.Helpers;

namespace HISWEBAPI.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly ICustomSqlHelper _sqlHelper;

        public UserRepository(ICustomSqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper;
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
                new SqlParameter("@Password", request.Password),
                new SqlParameter("@UserName", request.UserName),
                new SqlParameter("@Gender", request.Gender ?? (object)DBNull.Value),
                new SqlParameter("@UserId", request.UserId.HasValue && request.UserId.Value > 0
                    ? (object)request.UserId.Value
                    : DBNull.Value), // Fixed: proper null handling
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
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@UserId", userId),
            new SqlParameter("@Otp", otp),
            new SqlParameter("@ExpiryMinutes", expiryMinutes)
                };

                _sqlHelper.RunProcedure("sp_StoreOtpForPasswordReset", parameters);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}