using HISWEBAPI.DTO;

namespace HISWEBAPI.Interface
{
    public interface IUserRepository
    {
        long InsertUserMaster(UserMasterRequest request);
    }
}
