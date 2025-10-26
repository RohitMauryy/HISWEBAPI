using HISWEBAPI.DTO.User;

namespace HISWEBAPI.Repositories.Interfaces
{
    public interface IUserRepository
    {
        long InsertUserMaster(UserMasterRequest request);
    }
}
