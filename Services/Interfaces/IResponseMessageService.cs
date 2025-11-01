using HISWEBAPI.Models;
using HISWEBAPI.DTO;


namespace HISWEBAPI.Services
{
    public interface IResponseMessageService
    {
        (string Type, string Message) GetMessageAndTypeByAlertCode(string alertCode);
        string CreateUpdateResponseMessage(ResponseMessageRequest request, AllGlobalValues globalValues);


    }
}