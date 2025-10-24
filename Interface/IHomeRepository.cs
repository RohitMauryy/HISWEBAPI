using System.Collections.Generic;
using System.Threading.Tasks;
using HISWEBAPI.Models;

namespace HISWEBAPI.Interface
{
    public interface IHomeRepository
    {
        Task<IEnumerable<BranchModel>> GetActiveBranchListAsync();
        Task<long> UserLoginAsync(int branchId, string userName, string password);
    }
}
