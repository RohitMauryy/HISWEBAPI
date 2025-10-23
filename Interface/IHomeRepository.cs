using HISWEBAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HISWEBAPI.Interface
{
    public interface IHomeRepository
    {
        Task<IEnumerable<LoginModel>> UserLoginAsync(int branchId, string userName, string password);
    }
}
