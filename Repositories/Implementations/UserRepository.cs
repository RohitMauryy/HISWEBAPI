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
    }
}