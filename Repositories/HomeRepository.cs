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

        /// <summary>
        /// Fetch active branch list from the database.
        /// </summary>
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
                                branchName = reader["BranchName"].ToString()
                            });
                        }
                    }
                }
            }

            return branches;
        }

        /// <summary>
        /// Validate user credentials and return UserId if valid.
        /// </summary>
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
                    {
                        return userId; // Login success — return UserId or status code
                    }

                    return 0; // Invalid credentials or no data found
                }
            }
        }
    }
}
