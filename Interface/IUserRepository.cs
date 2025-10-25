using HISWEBAPI.DTO;
using System.Threading.Tasks;

namespace HISWEBAPI.Interface
{
    public interface IUserRepository
    {
        Task<long> InsertUserMasterAsync(UserMasterRequest request);
    }
}