using HISWEBAPI.Data;
using HISWEBAPI.Models;
using HISWEBAPI.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HISWEBAPI.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public HomeRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<BranchModel>> getActiveBranchListAsync()
        {
            // Use strongly typed DbSet
            return await _dbContext.BranchDetails
                .FromSqlRaw("EXEC [dbo].[S_GetActiveBranchList]")
                .ToListAsync();
        }

        public async Task<IEnumerable<LoginModel>> UserLoginAsync(int branchId, string userName, string password)
        {
            var result = await _dbContext.LoginDetails
                .FromSqlRaw("EXEC [dbo].[sp_S_Login] @BranchId = {0}, @UserName = {1}, @Password = {2}",
                            branchId, userName, password)
                .ToListAsync();

            return result;
        }
    }
}
