using HISWEBAPI.Models;
using HISWEBAPI.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System;

namespace HISWEBAPI.Repositories
{
    public class HomeRepository : Repository, IHomeRepository
    {
        public HomeRepository(IConfiguration configuration) : base(configuration)
        {
        }

       
        public async Task<IEnumerable<BranchModel>> GetActiveBranchListAsync()
        {
            var branches = new List<BranchModel>();

            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("S_GetActiveBranchList", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            branches.Add(new BranchModel
                            {
                                branchId = Convert.ToInt32(reader["BranchId"]),
                                branchName = (string)reader["BranchName"]
                            });
                        }
                    }
                }
            }

            return branches;
        }

      
        public async Task<long> UserLoginAsync(int branchId, string userName, string password)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("sp_S_Login", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@BranchId", branchId);
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    cmd.Parameters.AddWithValue("@Password", password);

                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null && long.TryParse(result.ToString(), out long userId))
                        return userId;

                    return 0; // invalid credentials
                }
            }
        }
    }
}
