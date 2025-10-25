using HISWEBAPI.DTO;
using HISWEBAPI.Interface;
using PMS.DAL;
using System.Data;
using Microsoft.Data.SqlClient;

namespace HISWEBAPI.Repositories
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
                new SqlParameter("@Contact", request.Contact),
                new SqlParameter("@DOB", request.DOB),
                new SqlParameter("@Email", request.Email),
                new SqlParameter("@FirstName", request.FirstName),
                new SqlParameter("@MidelName", request.MiddleName ?? (object)DBNull.Value),
                new SqlParameter("@LastName", request.LastName),
                new SqlParameter("@Password", request.Password),
                new SqlParameter("@UserName", request.UserName),
                new SqlParameter("@Gender", request.Gender),
                new SqlParameter("@UserId", request.UserId ?? (object)DBNull.Value),
                new SqlParameter("@IsActive", request.IsActive),
                new SqlParameter("@EmployeeID", request.EmployeeID ?? (object)DBNull.Value),
                new SqlParameter("@Result", SqlDbType.BigInt) { Direction = ParameterDirection.Output }
            };

            return _sqlHelper.RunProcedureInsert("sp_I_UserMaster", parameters);
        }
    }
}
